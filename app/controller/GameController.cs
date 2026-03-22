namespace app.controller;

using app.model;
using app.service;

public class GameController
{
    private readonly GameService _gameService;
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

    public GameController()
    {
        _gameService = new GameService();
        _game = _gameService.CreateNewGame(10, 10);
    }

    public void NewGame(int width, int height)
    {
        _game = _gameService.CreateNewGame(width, height);
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

    public bool HandleClick(int clickX, int clickY, int margin, int cellSize)
    {
        // Si le joueur a deplace le canon, il ne peut plus placer de points
        if (_game.CurrentPlayerHasDraggedCannon)
            return false;

        int tolerance = cellSize / 3;
        Player playerBeforeMove = _game.CurrentPlayer;

        var intersection = _gameService.GetNearestIntersection(
            clickX, clickY,
            margin, cellSize,
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

        var result = _gameService.FireCannon(_game, speed);

        if (result != null)
        {
            OnCannonFired?.Invoke(result);
            OnGameUpdated?.Invoke();
            OnPlayerChanged?.Invoke(_game.CurrentPlayer);
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

    public string GetScoreText()
    {
        var scores = string.Join(" | ", _game.Players.Select(p => $"{p.Name}: {p.Score}"));
        return scores;
    }
}
