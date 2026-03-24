namespace app.controller;

using app.model;
using app.service;

public class GameController
{
    private readonly GameService _gameService;
    private readonly GameSaveService _saveService;
    private Game _game;

    // Events existants
    public event Action? OnGameUpdated;
    public event Action<Player>? OnPlayerChanged;
    public event Action<Player, int>? OnScoreChanged;

    // Events pour le canon
    public event Action<Cannon>? OnCannonMoved;
    public event Action<CannonFireResult>? OnCannonFired;
    public event Action<Cannon>? OnCannonDragStarted;
    public event Action<Cannon>? OnCannonDragEnded;

    // Events pour la sauvegarde
    public event Action<string>? OnSaveStatusChanged;
    public event Action<string>? OnSaveError;

    // Proprietes existantes
    public Game CurrentGame => _game;
    public Player CurrentPlayer => _game.CurrentPlayer;
    public Board Board => _game.Board;
    public List<Player> Players => _game.Players;
    public List<Line> ScoredLines => _game.ScoredLines;

    // Proprietes pour le canon
    public Cannon? Player1Cannon => _game.Players.Count > 0 ? _game.Players[0].Cannon : null;
    public Cannon? Player2Cannon => _game.Players.Count > 1 ? _game.Players[1].Cannon : null;
    public Cannon? CurrentPlayerCannon => _game.CurrentPlayer.Cannon;
    public bool HasDraggedCannonThisTurn => _game.CurrentPlayerHasDraggedCannon;

    // Proprietes pour la sauvegarde
    public bool AutoSaveEnabled => _saveService.AutoSaveEnabled;
    public int CurrentGameSaveId => _saveService.CurrentGameSaveId;
    public int CurrentTurnNumber => _saveService.CurrentTurnNumber;
    public GameSaveService SaveService => _saveService;

    public GameController()
    {
        _gameService = new GameService();
        _saveService = new GameSaveService();
        _game = _gameService.CreateNewGame(10, 10);
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            await _saveService.InitializeDatabaseAsync();
            OnSaveStatusChanged?.Invoke("Base de donnees initialisee");
        }
        catch (Exception ex)
        {
            OnSaveError?.Invoke($"Erreur DB: {ex.Message}");
        }
    }

    public void NewGame(int width, int height)
    {
        _game = _gameService.CreateNewGame(width, height);
        _saveService.Reset();
        OnGameUpdated?.Invoke();
        OnPlayerChanged?.Invoke(_game.CurrentPlayer);
    }

    public void ResetGame()
    {
        _gameService.ResetGame(_game);
        OnGameUpdated?.Invoke();
        OnPlayerChanged?.Invoke(_game.CurrentPlayer);
    }

    public void ResizeBoard(int width, int height)
    {
        _gameService.ResizeGame(_game, width, height);
        OnGameUpdated?.Invoke();
        OnPlayerChanged?.Invoke(_game.CurrentPlayer);
    }

    public bool HandleClick(int clickX, int clickY, int marginLeft, int marginTop, int cellSize)
    {
        // Si le joueur a deplace le canon, il ne peut plus placer de points
        if (_game.CurrentPlayerHasDraggedCannon)
            return false;

        int tolerance = cellSize / 3;
        Player playerBeforeMove = _game.CurrentPlayer;

        var intersection = _gameService.GetNearestIntersection(
            clickX, clickY,
            marginLeft, marginTop, cellSize,
            _game.Board.Width, _game.Board.Height,
            tolerance);

        if (intersection.HasValue)
        {
            int scoreBefore = playerBeforeMove.Score;
            bool placed = _gameService.PlacePoint(_game, intersection.Value.gridX, intersection.Value.gridY);

            if (placed)
            {
                OnGameUpdated?.Invoke();
                OnPlayerChanged?.Invoke(_game.CurrentPlayer);

                // Vérifier si le score a changé
                if (playerBeforeMove.Score > scoreBefore)
                {
                    OnScoreChanged?.Invoke(playerBeforeMove, playerBeforeMove.Score);
                }

                // Auto-save si active
                if (_saveService.AutoSaveEnabled && _saveService.CurrentGameSaveId > 0)
                {
                    _ = AutoSaveAsync($"POINT_PLACE", $"{playerBeforeMove.Name} place ({intersection.Value.gridX},{intersection.Value.gridY})");
                }

                return true;
            }
        }

        return false;
    }

    // ============ CANNON METHODS ============

    /// <summary>
    /// Commence le glissement du canon du joueur actuel.
    /// </summary>
    public bool StartCannonDrag()
    {
        var cannon = _game.CurrentPlayer.Cannon;
        if (cannon == null || _game.IsFinished)
            return false;

        cannon.IsDragging = true;
        _game.CurrentPlayerHasDraggedCannon = true;
        OnCannonDragStarted?.Invoke(cannon);
        return true;
    }

    /// <summary>
    /// Deplace le canon pendant le glissement.
    /// </summary>
    public bool MoveCannonTo(int gridY)
    {
        var cannon = _game.CurrentPlayer.Cannon;
        if (cannon == null || !cannon.IsDragging)
            return false;

        if (_gameService.CannonService.MoveCannon(cannon, gridY, _game.Board.Height))
        {
            OnCannonMoved?.Invoke(cannon);
            OnGameUpdated?.Invoke();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Termine le glissement du canon.
    /// </summary>
    public void EndCannonDrag()
    {
        var cannon = _game.CurrentPlayer.Cannon;
        if (cannon != null)
        {
            cannon.IsDragging = false;
            OnCannonDragEnded?.Invoke(cannon);
        }
    }

    /// <summary>
    /// Tire le canon du joueur actuel.
    /// </summary>
    public CannonFireResult? FireCannon(int speed)
    {
        if (_game.IsFinished)
            return null;

        // Doit avoir deplace le canon ce tour pour tirer
        if (!_game.CurrentPlayerHasDraggedCannon)
            return null;

        // La vitesse doit etre entre 1 et 9
        if (speed < 1 || speed > 9)
            return null;

        Player playerBeforeFire = _game.CurrentPlayer;
        var result = _gameService.FireCannon(_game, speed);

        if (result != null)
        {
            OnCannonFired?.Invoke(result);
            OnGameUpdated?.Invoke();
            OnPlayerChanged?.Invoke(_game.CurrentPlayer);

            // Auto-save si active
            if (_saveService.AutoSaveEnabled && _saveService.CurrentGameSaveId > 0)
            {
                string desc = result.WasDestroyed
                    ? $"{playerBeforeFire.Name} detruit point en ({result.HitPoint?.X},{result.HitPoint?.Y})"
                    : $"{playerBeforeFire.Name} tire (vitesse {speed})";
                _ = AutoSaveAsync("CANNON_FIRE", desc);
            }
        }

        return result;
    }

    /// <summary>
    /// Verifie si un clic est sur la zone du canon du joueur actuel.
    /// </summary>
    public bool IsClickOnCannonArea(int clickX, int margin, int cellSize)
    {
        var cannon = _game.CurrentPlayer.Cannon;
        if (cannon == null)
            return false;

        if (cannon.Side == CannonSide.Left)
        {
            // Zone du canon gauche : x < margin
            return clickX < margin;
        }
        else
        {
            // Zone du canon droite : x > bord droit de la grille
            int gridRight = margin + (_game.Board.Width - 1) * cellSize;
            return clickX > gridRight;
        }
    }

    // ============ SAVE/LOAD METHODS ============

    /// <summary>
    /// Cree une nouvelle sauvegarde de la partie
    /// </summary>
    public async Task<int> CreateSaveAsync(string name, bool autoSave = false)
    {
        try
        {
            var id = await _saveService.CreateGameSaveAsync(_game, name, autoSave);
            OnSaveStatusChanged?.Invoke($"Partie sauvegardee: {name}");
            return id;
        }
        catch (Exception ex)
        {
            OnSaveError?.Invoke($"Erreur sauvegarde: {ex.Message}");
            return -1;
        }
    }

    /// <summary>
    /// Sauvegarde manuelle de l'etat actuel
    /// </summary>
    public async Task<bool> ManualSaveAsync(string? description = null)
    {
        if (_saveService.CurrentGameSaveId == 0)
        {
            OnSaveError?.Invoke("Aucune partie en cours. Creez d'abord une sauvegarde.");
            return false;
        }

        try
        {
            await _saveService.SaveTurnStateAsync(_game, "MANUAL_SAVE", description ?? "Sauvegarde manuelle");
            OnSaveStatusChanged?.Invoke($"Tour {_saveService.CurrentTurnNumber} sauvegarde");
            return true;
        }
        catch (Exception ex)
        {
            OnSaveError?.Invoke($"Erreur: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Auto-save apres une action
    /// </summary>
    private async Task AutoSaveAsync(string actionType, string description)
    {
        try
        {
            await _saveService.SaveTurnStateAsync(_game, actionType, description);
        }
        catch
        {
            // Silencieux pour l'auto-save
        }
    }

    /// <summary>
    /// Active/desactive l'auto-save
    /// </summary>
    public void SetAutoSave(bool enabled)
    {
        _saveService.AutoSaveEnabled = enabled;
        OnSaveStatusChanged?.Invoke(enabled ? "Auto-save active" : "Auto-save desactive");
    }

    /// <summary>
    /// Charge la liste des parties sauvegardees
    /// </summary>
    public async Task<List<GameSave>> GetSavedGamesAsync()
    {
        try
        {
            return await _saveService.GetAllGameSavesAsync();
        }
        catch (Exception ex)
        {
            OnSaveError?.Invoke($"Erreur: {ex.Message}");
            return new List<GameSave>();
        }
    }

    /// <summary>
    /// Charge la liste des tours pour une partie
    /// </summary>
    public async Task<List<TurnState>> GetTurnStatesAsync(int gameId)
    {
        try
        {
            return await _saveService.GetTurnStatesForGameAsync(gameId);
        }
        catch (Exception ex)
        {
            OnSaveError?.Invoke($"Erreur: {ex.Message}");
            return new List<TurnState>();
        }
    }

    /// <summary>
    /// Charge un etat de tour specifique
    /// </summary>
    public async Task<bool> LoadTurnStateAsync(int turnStateId)
    {
        try
        {
            _game = await _saveService.LoadTurnStateAsync(turnStateId);
            OnGameUpdated?.Invoke();
            OnPlayerChanged?.Invoke(_game.CurrentPlayer);
            OnSaveStatusChanged?.Invoke($"Tour {_saveService.CurrentTurnNumber} charge");
            return true;
        }
        catch (Exception ex)
        {
            OnSaveError?.Invoke($"Erreur chargement: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Supprime une partie sauvegardee
    /// </summary>
    public async Task<bool> DeleteSaveAsync(int gameId)
    {
        try
        {
            await _saveService.DeleteGameSaveAsync(gameId);
            OnSaveStatusChanged?.Invoke("Sauvegarde supprimee");
            return true;
        }
        catch (Exception ex)
        {
            OnSaveError?.Invoke($"Erreur: {ex.Message}");
            return false;
        }
    }

    public string GetScoreText()
    {
        var scores = string.Join(" | ", _game.Players.Select(p => $"{p.Name}: {p.Score}"));
        return scores;
    }
}
