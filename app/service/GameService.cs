namespace app.service;

using app.model;
using System.Drawing;

public class GameService
{
    private static readonly (int dx, int dy)[] Directions = new[]
    {
        (1, 0),   // Horizontal
        (0, 1),   // Vertical
        (1, 1),   // Diagonale \
        (1, -1)   // Diagonale /
    };

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
            // Vérifier si une ligne de 5 est formée
            CheckForLines(game, x, y, game.CurrentPlayer);
            game.NextTurn();
        }

        return placed;
    }

    public void CheckForLines(Game game, int x, int y, Player player)
    {
        foreach (var (dx, dy) in Directions)
        {
            var linePoints = GetLinePoints(game.Board, x, y, dx, dy, player);

            if (linePoints.Count >= 5)
            {
                // Chercher des segments de 5 points non encore comptés
                var newLines = FindNewLines(linePoints, game);
                foreach (var line in newLines)
                {
                    game.AddScoredLine(line);
                    // Marquer les points comme faisant partie d'une ligne scorée
                    foreach (var point in line.Points)
                    {
                        point.IsPartOfScoredLine = true;
                    }
                }
            }
        }
    }

    private List<Point> GetLinePoints(Board board, int startX, int startY, int dx, int dy, Player player)
    {
        var points = new List<Point>();

        // Chercher dans la direction négative
        int x = startX - dx;
        int y = startY - dy;
        var tempPoints = new List<Point>();

        while (board.IsValidPosition(x, y))
        {
            var point = board.GetPoint(x, y);
            if (point != null && point.Owner == player)
            {
                tempPoints.Add(point);
                x -= dx;
                y -= dy;
            }
            else
            {
                break;
            }
        }

        // Inverser pour avoir l'ordre correct
        tempPoints.Reverse();
        points.AddRange(tempPoints);

        // Ajouter le point de départ
        var startPoint = board.GetPoint(startX, startY);
        if (startPoint != null)
        {
            points.Add(startPoint);
        }

        // Chercher dans la direction positive
        x = startX + dx;
        y = startY + dy;

        while (board.IsValidPosition(x, y))
        {
            var point = board.GetPoint(x, y);
            if (point != null && point.Owner == player)
            {
                points.Add(point);
                x += dx;
                y += dy;
            }
            else
            {
                break;
            }
        }

        return points;
    }

    private List<Line> FindNewLines(List<Point> linePoints, Game game)
    {
        var newLines = new List<Line>();

        // Parcourir tous les segments possibles de 5 points
        for (int i = 0; i <= linePoints.Count - 5; i++)
        {
            var segment = linePoints.Skip(i).Take(5).ToList();

            // Vérifier si ce segment contient des points déjà utilisés dans une ligne
            bool allPointsAvailable = segment.All(p => !p.IsPartOfScoredLine);

            if (allPointsAvailable)
            {
                var line = new Line(segment, segment[0].Owner!);
                newLines.Add(line);

                // Note: On ne marque pas encore les points comme utilisés
                // Car on veut permettre plusieurs lignes différentes
                // seulement si elles sont dans des directions différentes
                break; // Une seule ligne par direction pour cette vérification
            }
        }

        return newLines;
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
