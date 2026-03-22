namespace app.service;

using app.model;
using System.Drawing;
using Point = model.Point;

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

        // Vérifier si le point couperait une ligne de l'adversaire
        if (WouldCrossOpponentLine(game, x, y, game.CurrentPlayer))
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

    private bool WouldCrossOpponentLine(Game game, int x, int y, Player currentPlayer)
    {
        foreach (var line in game.ScoredLines)
        {
            // On peut couper ses propres lignes
            if (line.Owner == currentPlayer)
                continue;

            // Vérifier si le point (x, y) est sur le segment de la ligne adverse
            if (IsPointOnLineSegment(x, y, line))
                return true;
        }
        return false;
    }

    private bool IsPointOnLineSegment(int x, int y, Line line)
    {
        if (line.Points.Count < 2)
            return false;

        var first = line.Points.First();
        var last = line.Points.Last();

        // Calculer la direction de la ligne
        int dx = Math.Sign(last.X - first.X);
        int dy = Math.Sign(last.Y - first.Y);

        // Vérifier si le point est aligné avec la ligne
        int diffX = x - first.X;
        int diffY = y - first.Y;

        // Si la ligne est horizontale (dy == 0)
        if (dy == 0 && dx != 0)
        {
            if (y != first.Y) return false;
            int minX = Math.Min(first.X, last.X);
            int maxX = Math.Max(first.X, last.X);
            return x > minX && x < maxX;
        }

        // Si la ligne est verticale (dx == 0)
        if (dx == 0 && dy != 0)
        {
            if (x != first.X) return false;
            int minY = Math.Min(first.Y, last.Y);
            int maxY = Math.Max(first.Y, last.Y);
            return y > minY && y < maxY;
        }

        // Ligne diagonale
        if (dx != 0 && dy != 0)
        {
            // Vérifier que le point est sur la diagonale
            if (diffX * dy != diffY * dx) return false;

            // Vérifier que le point est entre les extrémités (exclusif)
            int minX = Math.Min(first.X, last.X);
            int maxX = Math.Max(first.X, last.X);
            int minY = Math.Min(first.Y, last.Y);
            int maxY = Math.Max(first.Y, last.Y);

            return x > minX && x < maxX && y > minY && y < maxY;
        }

        return false;
    }

    public void CheckForLines(Game game, int x, int y, Player player)
    {
        foreach (var (dx, dy) in Directions)
        {
            var linePoints = GetLinePoints(game.Board, x, y, dx, dy, player);

            if (linePoints.Count >= 5)
            {
                // Chercher des segments de 5 points non encore comptés dans cette direction
                var newLines = FindNewLines(linePoints, game, dx, dy);
                foreach (var line in newLines)
                {
                    game.AddScoredLine(line);
                    // Marquer les points comme faisant partie d'une ligne scorée dans cette direction
                    foreach (var point in line.Points)
                    {
                        point.MarkScoredInDirection(dx, dy);
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
            // Un joueur ne peut utiliser que ses propres points
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
            // Un joueur ne peut utiliser que ses propres points
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

    private List<Line> FindNewLines(List<Point> linePoints, Game game, int dx, int dy)
    {
        var newLines = new List<Line>();

        // Parcourir tous les segments possibles de 5 points
        for (int i = 0; i <= linePoints.Count - 5; i++)
        {
            var segment = linePoints.Skip(i).Take(5).ToList();

            // Vérifier si ce segment contient des points déjà utilisés dans CETTE direction
            bool allPointsAvailable = segment.All(p => !p.IsScoredInDirection(dx, dy));

            if (allPointsAvailable)
            {
                var line = new Line(segment, segment[0].Owner!);

                // Vérifier que cette ligne ne coupe pas une ligne adverse
                if (!LineIntersectsOpponentLines(line, game))
                {
                    newLines.Add(line);
                    break; // Trouvé une ligne valide, on arrête pour cette direction
                }
                // Si elle coupe une ligne adverse, continuer à chercher d'autres segments
            }
        }

        return newLines;
    }

    private bool LineIntersectsOpponentLines(Line newLine, Game game)
    {
        if (newLine.Points.Count < 2)
            return false;

        var first = newLine.Points.First();
        var last = newLine.Points.Last();

        foreach (var existingLine in game.ScoredLines)
        {
            // On peut croiser ses propres lignes
            if (existingLine.Owner == newLine.Owner)
                continue;

            // Vérifier si les deux lignes se croisent
            if (DoLinesIntersect(first, last, existingLine.Points.First(), existingLine.Points.Last()))
                return true;
        }

        return false;
    }

    private bool DoLinesIntersect(Point line1Start, Point line1End, Point line2Start, Point line2End)
    {
        // Utiliser la détection d'intersection géométrique de segments
        double x1 = line1Start.X, y1 = line1Start.Y;
        double x2 = line1End.X, y2 = line1End.Y;
        double x3 = line2Start.X, y3 = line2Start.Y;
        double x4 = line2End.X, y4 = line2End.Y;

        // Calculer le dénominateur
        double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);

        // Si denom == 0, les lignes sont parallèles
        if (Math.Abs(denom) < 0.0001)
            return false;

        // Calculer les paramètres t et u
        double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
        double u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / denom;

        // Les segments se croisent si t et u sont strictement entre 0 et 1 (exclusif)
        const double epsilon = 0.0001;
        return t > epsilon && t < (1 - epsilon) && u > epsilon && u < (1 - epsilon);
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
