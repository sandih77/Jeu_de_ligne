namespace app.service;

using app.model;
using System.Drawing;

public class GameService
{
    public Game CreateNewGame(int boardWidth, int boardHeight)
    {
        var players = CreateDefaultPlayers();
        return new Game(boardWidth, boardHeight, players);
    }

    public List<Player> CreateDefaultPlayers()
    {
        return new List<Player>
        {
            new Player(1, "Joueur 1", Color.Blue),
            new Player(2, "Joueur 2", Color.Red)
        };
    }

    public bool PlacePoint(Game game, int x, int y)
    {
        if (game.IsFinished)
            return false;

        bool placed = game.Board.PlacePoint(x, y, game.CurrentPlayer);

        if (placed)
        {
            game.NextTurn();
        }

        return placed;
    }

    public void ResetGame(Game game)
    {
        game.Reset();
    }

    public void ResizeGame(Game game, int width, int height)
    {
        game.Resize(width, height);
    }

    public (int gridX, int gridY)? GetNearestIntersection(
        int clickX, int clickY,
        int margin, int cellSize,
        int gridWidth, int gridHeight,
        int tolerance)
    {
        int relativeX = clickX - margin;
        int relativeY = clickY - margin;

        int gridPosX = (int)Math.Round((double)relativeX / cellSize);
        int gridPosY = (int)Math.Round((double)relativeY / cellSize);

        int exactX = gridPosX * cellSize;
        int exactY = gridPosY * cellSize;

        if (Math.Abs(relativeX - exactX) <= tolerance &&
            Math.Abs(relativeY - exactY) <= tolerance)
        {
            if (gridPosX >= 0 && gridPosX < gridWidth &&
                gridPosY >= 0 && gridPosY < gridHeight)
            {
                return (gridPosX, gridPosY);
            }
        }

        return null;
    }
}
