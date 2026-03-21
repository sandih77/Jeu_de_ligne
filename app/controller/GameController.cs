namespace app.controller;

using app.model;
using app.service;

public class GameController
{
    private readonly GameService _gameService;
    private Game _game;

    public event Action? OnGameUpdated;
    public event Action<Player>? OnPlayerChanged;

    public Game CurrentGame => _game;
    public Player CurrentPlayer => _game.CurrentPlayer;
    public Board Board => _game.Board;

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
        int tolerance = cellSize / 3;

        var intersection = _gameService.GetNearestIntersection(
            clickX, clickY,
            margin, cellSize,
            _game.Board.Width, _game.Board.Height,
            tolerance);

        if (intersection.HasValue)
        {
            bool placed = _gameService.PlacePoint(_game, intersection.Value.gridX, intersection.Value.gridY);

            if (placed)
            {
                OnGameUpdated?.Invoke();
                OnPlayerChanged?.Invoke(_game.CurrentPlayer);
                return true;
            }
        }

        return false;
    }
}
