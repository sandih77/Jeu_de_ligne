namespace app.service;

using Point = app.model.Point;
using app.model;
using app.utils;
using Npgsql;
using System.Drawing;

public class GameSaveService
{
    private int _currentGameSaveId = 0;
    private int _currentTurnNumber = 0;
    private bool _autoSaveEnabled = false;

    public bool AutoSaveEnabled
    {
        get => _autoSaveEnabled;
        set => _autoSaveEnabled = value;
    }

    public int CurrentGameSaveId => _currentGameSaveId;
    public int CurrentTurnNumber => _currentTurnNumber;

    public async Task InitializeDatabaseAsync()
    {
        await UtilsDB.InitializeDatabaseAsync();
    }

    // ============ SAVE METHODS ============

    /// <summary>
    /// Cree une nouvelle partie sauvegardee
    /// </summary>
    public async Task<int> CreateGameSaveAsync(Game game, string name, bool autoSave = false)
    {
        using var conn = UtilsDB.GetConnection();
        await conn.OpenAsync();

        // Creer la partie
        var sql = @"
            INSERT INTO game_saves (name, board_width, board_height, is_finished, auto_save)
            VALUES (@name, @width, @height, @finished, @auto)
            RETURNING id";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("name", name);
        cmd.Parameters.AddWithValue("width", game.Board.Width);
        cmd.Parameters.AddWithValue("height", game.Board.Height);
        cmd.Parameters.AddWithValue("finished", game.IsFinished);
        cmd.Parameters.AddWithValue("auto", autoSave);

        var gameId = (int)(await cmd.ExecuteScalarAsync())!;

        // Sauvegarder les joueurs
        foreach (var player in game.Players)
        {
            await SavePlayerAsync(conn, gameId, player);
        }

        _currentGameSaveId = gameId;
        _currentTurnNumber = 0;
        _autoSaveEnabled = autoSave;

        // Sauvegarder l'etat initial
        await SaveTurnStateAsync(game, "INITIAL", "Debut de partie");

        return gameId;
    }

    private async Task SavePlayerAsync(NpgsqlConnection conn, int gameId, Player player)
    {
        var sql = @"
            INSERT INTO game_players (game_save_id, player_id, name, color_r, color_g, color_b, score, cannon_side, cannon_position_y)
            VALUES (@gameId, @playerId, @name, @r, @g, @b, @score, @side, @cannonY)";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("gameId", gameId);
        cmd.Parameters.AddWithValue("playerId", player.Id);
        cmd.Parameters.AddWithValue("name", player.Name);
        cmd.Parameters.AddWithValue("r", (int)player.Color.R);
        cmd.Parameters.AddWithValue("g", (int)player.Color.G);
        cmd.Parameters.AddWithValue("b", (int)player.Color.B);
        cmd.Parameters.AddWithValue("score", player.Score);
        cmd.Parameters.AddWithValue("side", player.Cannon?.Side.ToString() ?? "Left");
        cmd.Parameters.AddWithValue("cannonY", player.Cannon?.PositionY ?? 0);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Sauvegarde l'etat actuel du tour
    /// </summary>
    public async Task<int> SaveTurnStateAsync(Game game, string actionType, string? description = null)
    {
        if (_currentGameSaveId == 0)
            throw new InvalidOperationException("Aucune partie en cours. Utilisez CreateGameSaveAsync d'abord.");

        using var conn = UtilsDB.GetConnection();
        await conn.OpenAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            _currentTurnNumber++;

            // Creer l'etat du tour
            var sql = @"
                INSERT INTO turn_states (game_save_id, turn_number, current_player_id, action_type, description)
                VALUES (@gameId, @turn, @playerId, @action, @desc)
                RETURNING id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gameId", _currentGameSaveId);
            cmd.Parameters.AddWithValue("turn", _currentTurnNumber);
            cmd.Parameters.AddWithValue("playerId", game.CurrentPlayer.Id);
            cmd.Parameters.AddWithValue("action", actionType);
            cmd.Parameters.AddWithValue("desc", description ?? (object)DBNull.Value);

            var turnStateId = (int)(await cmd.ExecuteScalarAsync())!;

            // Sauvegarder les points
            await SavePointsAsync(conn, turnStateId, game.Board);

            // Sauvegarder les lignes
            await SaveLinesAsync(conn, turnStateId, game.ScoredLines);

            // Sauvegarder les scores
            await SaveScoresAsync(conn, turnStateId, game.Players);

            // Mettre a jour updated_at
            var updateSql = "UPDATE game_saves SET updated_at = CURRENT_TIMESTAMP WHERE id = @id";
            using var updateCmd = new NpgsqlCommand(updateSql, conn);
            updateCmd.Parameters.AddWithValue("id", _currentGameSaveId);
            await updateCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return turnStateId;
        }
        catch
        {
            await transaction.RollbackAsync();
            _currentTurnNumber--;
            throw;
        }
    }

    private async Task SavePointsAsync(NpgsqlConnection conn, int turnStateId, Board board)
    {
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                var point = board.GetPoint(x, y);
                if (point != null && !point.IsEmpty)
                {
                    var sql = @"
                        INSERT INTO turn_state_points (turn_state_id, x, y, owner_player_id, is_part_of_scored_line)
                        VALUES (@turnId, @x, @y, @ownerId, @scored)
                        RETURNING id";

                    using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("turnId", turnStateId);
                    cmd.Parameters.AddWithValue("x", x);
                    cmd.Parameters.AddWithValue("y", y);
                    cmd.Parameters.AddWithValue("ownerId", point.Owner!.Id);
                    cmd.Parameters.AddWithValue("scored", point.IsPartOfScoredLine);

                    var pointId = (int)(await cmd.ExecuteScalarAsync())!;

                    // Sauvegarder les directions scorees
                    foreach (var dir in point.ScoredDirections)
                    {
                        var dirSql = @"
                            INSERT INTO point_scored_directions (turn_state_point_id, dx, dy)
                            VALUES (@pointId, @dx, @dy)";

                        using var dirCmd = new NpgsqlCommand(dirSql, conn);
                        dirCmd.Parameters.AddWithValue("pointId", pointId);
                        dirCmd.Parameters.AddWithValue("dx", dir.dx);
                        dirCmd.Parameters.AddWithValue("dy", dir.dy);
                        await dirCmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }
    }

    private async Task SaveLinesAsync(NpgsqlConnection conn, int turnStateId, List<Line> lines)
    {
        foreach (var line in lines)
        {
            var sql = @"
                INSERT INTO turn_state_lines (turn_state_id, owner_player_id, is_scored)
                VALUES (@turnId, @ownerId, @scored)
                RETURNING id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("turnId", turnStateId);
            cmd.Parameters.AddWithValue("ownerId", line.Owner.Id);
            cmd.Parameters.AddWithValue("scored", line.IsScored);

            var lineId = (int)(await cmd.ExecuteScalarAsync())!;

            // Sauvegarder les points de la ligne
            int order = 0;
            foreach (var point in line.Points)
            {
                var pointSql = @"
                    INSERT INTO line_points (turn_state_line_id, x, y, point_order)
                    VALUES (@lineId, @x, @y, @order)";

                using var pointCmd = new NpgsqlCommand(pointSql, conn);
                pointCmd.Parameters.AddWithValue("lineId", lineId);
                pointCmd.Parameters.AddWithValue("x", point.X);
                pointCmd.Parameters.AddWithValue("y", point.Y);
                pointCmd.Parameters.AddWithValue("order", order++);
                await pointCmd.ExecuteNonQueryAsync();
            }
        }
    }

    private async Task SaveScoresAsync(NpgsqlConnection conn, int turnStateId, List<Player> players)
    {
        foreach (var player in players)
        {
            var sql = @"
                INSERT INTO turn_state_scores (turn_state_id, player_id, score)
                VALUES (@turnId, @playerId, @score)";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("turnId", turnStateId);
            cmd.Parameters.AddWithValue("playerId", player.Id);
            cmd.Parameters.AddWithValue("score", player.Score);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    // ============ LOAD METHODS ============

    /// <summary>
    /// Charge la liste des parties sauvegardees
    /// </summary>
    public async Task<List<GameSave>> GetAllGameSavesAsync()
    {
        var saves = new List<GameSave>();

        using var conn = UtilsDB.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT gs.id, gs.name, gs.board_width, gs.board_height, gs.created_at, gs.updated_at,
                   gs.is_finished, gs.winner_id, gs.auto_save,
                   (SELECT COUNT(*) FROM turn_states ts WHERE ts.game_save_id = gs.id) as turn_count
            FROM game_saves gs
            ORDER BY gs.updated_at DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            saves.Add(new GameSave
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                BoardWidth = reader.GetInt32(2),
                BoardHeight = reader.GetInt32(3),
                CreatedAt = reader.GetDateTime(4),
                UpdatedAt = reader.GetDateTime(5),
                IsFinished = reader.GetBoolean(6),
                WinnerId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                AutoSave = reader.GetBoolean(8),
                TurnCount = reader.GetInt32(9)
            });
        }

        return saves;
    }

    /// <summary>
    /// Charge la liste des etats de tour pour une partie
    /// </summary>
    public async Task<List<TurnState>> GetTurnStatesForGameAsync(int gameId)
    {
        var states = new List<TurnState>();

        using var conn = UtilsDB.GetConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT id, game_save_id, turn_number, current_player_id, saved_at, action_type, description
            FROM turn_states
            WHERE game_save_id = @gameId
            ORDER BY turn_number";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("gameId", gameId);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            states.Add(new TurnState
            {
                Id = reader.GetInt32(0),
                GameSaveId = reader.GetInt32(1),
                TurnNumber = reader.GetInt32(2),
                CurrentPlayerId = reader.GetInt32(3),
                SavedAt = reader.GetDateTime(4),
                ActionType = reader.GetString(5),
                Description = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }

        return states;
    }

    /// <summary>
    /// Charge un etat de tour specifique et restaure le jeu
    /// </summary>
    public async Task<Game> LoadTurnStateAsync(int turnStateId)
    {
        using var conn = UtilsDB.GetConnection();
        await conn.OpenAsync();

        // Charger les infos du turn state
        var turnSql = @"
            SELECT ts.game_save_id, ts.turn_number, ts.current_player_id,
                   gs.board_width, gs.board_height, gs.auto_save
            FROM turn_states ts
            JOIN game_saves gs ON ts.game_save_id = gs.id
            WHERE ts.id = @id";

        int gameId, turnNumber, currentPlayerId, boardWidth, boardHeight;
        bool autoSave;

        using (var cmd = new NpgsqlCommand(turnSql, conn))
        {
            cmd.Parameters.AddWithValue("id", turnStateId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new Exception("Turn state not found");

            gameId = reader.GetInt32(0);
            turnNumber = reader.GetInt32(1);
            currentPlayerId = reader.GetInt32(2);
            boardWidth = reader.GetInt32(3);
            boardHeight = reader.GetInt32(4);
            autoSave = reader.GetBoolean(5);
        }

        // Charger les joueurs
        var players = await LoadPlayersAsync(conn, gameId);

        // Creer le jeu
        var game = new Game(boardWidth, boardHeight, players);

        // Charger les points
        await LoadPointsAsync(conn, turnStateId, game, players);

        // Charger les lignes
        await LoadLinesAsync(conn, turnStateId, game, players);

        // Charger les scores
        await LoadScoresAsync(conn, turnStateId, players);

        // Definir le joueur actuel
        var currentPlayer = players.FirstOrDefault(p => p.Id == currentPlayerId);
        if (currentPlayer != null)
        {
            // Utiliser reflection ou propriete pour definir CurrentPlayer
            SetCurrentPlayer(game, currentPlayer);
        }

        // Mettre a jour l'etat du service
        _currentGameSaveId = gameId;
        _currentTurnNumber = turnNumber;
        _autoSaveEnabled = autoSave;

        return game;
    }

    private async Task<List<Player>> LoadPlayersAsync(NpgsqlConnection conn, int gameId)
    {
        var players = new List<Player>();

        var sql = @"
            SELECT player_id, name, color_r, color_g, color_b, score, cannon_side, cannon_position_y
            FROM game_players
            WHERE game_save_id = @gameId
            ORDER BY player_id";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("gameId", gameId);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var player = new Player(
                reader.GetInt32(0),
                reader.GetString(1),
                Color.FromArgb(reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4))
            );
            player.Score = reader.GetInt32(5);

            var cannonSide = reader.GetString(6) == "Left" ? CannonSide.Left : CannonSide.Right;
            player.Cannon = new Cannon(player, cannonSide, reader.GetInt32(7));

            players.Add(player);
        }

        return players;
    }

    private async Task LoadPointsAsync(NpgsqlConnection conn, int turnStateId, Game game, List<Player> players)
    {
        var sql = @"
            SELECT tsp.id, tsp.x, tsp.y, tsp.owner_player_id
            FROM turn_state_points tsp
            WHERE tsp.turn_state_id = @turnId";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("turnId", turnStateId);
        using var reader = await cmd.ExecuteReaderAsync();

        var pointsData = new List<(int id, int x, int y, int ownerId)>();
        while (await reader.ReadAsync())
        {
            pointsData.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3)));
        }
        reader.Close();

        foreach (var (id, x, y, ownerId) in pointsData)
        {
            var owner = players.FirstOrDefault(p => p.Id == ownerId);
            if (owner != null)
            {
                game.Board.PlacePoint(x, y, owner);

                // Charger les directions scorees
                var dirSql = @"
                    SELECT dx, dy FROM point_scored_directions WHERE turn_state_point_id = @pointId";
                using var dirCmd = new NpgsqlCommand(dirSql, conn);
                dirCmd.Parameters.AddWithValue("pointId", id);
                using var dirReader = await dirCmd.ExecuteReaderAsync();

                var point = game.Board.GetPoint(x, y);
                while (await dirReader.ReadAsync())
                {
                    point?.MarkScoredInDirection(dirReader.GetInt32(0), dirReader.GetInt32(1));
                }
            }
        }
    }

    private async Task LoadLinesAsync(NpgsqlConnection conn, int turnStateId, Game game, List<Player> players)
    {
        var sql = @"
            SELECT tsl.id, tsl.owner_player_id, tsl.is_scored
            FROM turn_state_lines tsl
            WHERE tsl.turn_state_id = @turnId";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("turnId", turnStateId);
        using var reader = await cmd.ExecuteReaderAsync();

        var linesData = new List<(int id, int ownerId, bool isScored)>();
        while (await reader.ReadAsync())
        {
            linesData.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetBoolean(2)));
        }
        reader.Close();

        foreach (var (lineId, ownerId, isScored) in linesData)
        {
            var owner = players.FirstOrDefault(p => p.Id == ownerId);
            if (owner == null) continue;

            // Charger les points de la ligne
            var pointsSql = @"
                SELECT x, y FROM line_points
                WHERE turn_state_line_id = @lineId
                ORDER BY point_order";
            using var pointsCmd = new NpgsqlCommand(pointsSql, conn);
            pointsCmd.Parameters.AddWithValue("lineId", lineId);
            using var pointsReader = await pointsCmd.ExecuteReaderAsync();

            var linePoints = new List<Point>();
            while (await pointsReader.ReadAsync())
            {
                var point = game.Board.GetPoint(pointsReader.GetInt32(0), pointsReader.GetInt32(1));
                if (point != null)
                    linePoints.Add(point);
            }

            if (linePoints.Count > 0)
            {
                var line = new Line(linePoints, owner);
                if (isScored) line.MarkAsScored();
                game.ScoredLines.Add(line);
            }
        }
    }

    private async Task LoadScoresAsync(NpgsqlConnection conn, int turnStateId, List<Player> players)
    {
        var sql = @"
            SELECT player_id, score FROM turn_state_scores WHERE turn_state_id = @turnId";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("turnId", turnStateId);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var playerId = reader.GetInt32(0);
            var score = reader.GetInt32(1);
            var player = players.FirstOrDefault(p => p.Id == playerId);
            if (player != null)
                player.Score = score;
        }
    }

    private void SetCurrentPlayer(Game game, Player player)
    {
        // Le jeu utilise CurrentPlayer en lecture seule, on doit faire des tours jusqu'au bon joueur
        while (game.CurrentPlayer.Id != player.Id)
        {
            game.NextTurn();
        }
    }

    /// <summary>
    /// Supprime une partie sauvegardee
    /// </summary>
    public async Task DeleteGameSaveAsync(int gameId)
    {
        using var conn = UtilsDB.GetConnection();
        await conn.OpenAsync();

        var sql = "DELETE FROM game_saves WHERE id = @id";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", gameId);
        await cmd.ExecuteNonQueryAsync();

        if (_currentGameSaveId == gameId)
        {
            _currentGameSaveId = 0;
            _currentTurnNumber = 0;
        }
    }

    /// <summary>
    /// Reinitialise l'etat du service (pour nouvelle partie sans sauvegarde)
    /// </summary>
    public void Reset()
    {
        _currentGameSaveId = 0;
        _currentTurnNumber = 0;
        _autoSaveEnabled = false;
    }
}
