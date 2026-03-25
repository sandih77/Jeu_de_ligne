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

    private readonly CannonService _cannonService;

    public CannonService CannonService => _cannonService;

    public GameService()
    {
        _cannonService = new CannonService();
    }

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
                // Calculer combien de points cette séquence rapporte
                int pointsEarned = CalculatePointsForSequence(linePoints.Count);

                // Vérifier si cette séquence existe déjà et combien elle rapportait avant
                int previousPoints = GetPreviousSequenceScore(linePoints, game, dx, dy);

                // Si cette séquence rapporte plus de points qu'avant, ajouter la différence
                int newPoints = pointsEarned - previousPoints;
                if (newPoints > 0)
                {
                    // Créer et ajouter les nouvelles lignes correspondant aux nouveaux points
                    AddNewScoredLines(linePoints, game, newPoints, dx, dy);
                }
            }
        }
    }

    private int CalculatePointsForSequence(int length)
    {
        if (length < 5)
            return 0;

        // Formule : (longueur - 1) / 4
        // 5 points → 1 ligne, 9 points → 2 lignes, 13 points → 3 lignes, etc.
        return (length - 1) / 4;
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

    private int GetPreviousSequenceScore(List<Point> linePoints, Game game, int dx, int dy)
    {
        if (linePoints.Count == 0)
            return 0;

        var firstPoint = linePoints.First();
        var lastPoint = linePoints.Last();

        // Chercher une séquence existante qui a les mêmes extrémités
        int totalScore = 0;
        foreach (var scoredLine in game.ScoredLines)
        {
            if (scoredLine.Owner != firstPoint.Owner)
                continue;

            // Vérifier si cette ligne fait partie de la même séquence
            // en vérifiant si elle a la même direction et est contenue dans notre séquence
            if (IsLineInSequence(scoredLine, linePoints, dx, dy))
            {
                totalScore++;
            }
        }

        return totalScore;
    }

    private bool IsLineInSequence(Line scoredLine, List<Point> sequence, int dx, int dy)
    {
        if (scoredLine.Points.Count == 0)
            return false;

        // Vérifier si tous les points de la ligne scorée sont dans la séquence
        foreach (var point in scoredLine.Points)
        {
            bool found = false;
            foreach (var seqPoint in sequence)
            {
                if (seqPoint.X == point.X && seqPoint.Y == point.Y)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                return false;
        }

        return true;
    }

    private void AddNewScoredLines(List<Point> linePoints, Game game, int newPoints, int dx, int dy)
    {
        // Ajouter les nouvelles lignes en progressant de 4 en 4
        // On commence à partir de la position qui correspond au score actuel
        int previousScore = GetPreviousSequenceScore(linePoints, game, dx, dy);
        int startIndex = previousScore * 4;

        for (int i = 0; i < newPoints; i++)
        {
            int segmentStart = startIndex + (i * 4);
            if (segmentStart + 5 <= linePoints.Count)
            {
                var segment = linePoints.Skip(segmentStart).Take(5).ToList();
                var line = new Line(segment, segment[0].Owner!);

                // Vérifier que cette ligne ne coupe pas une ligne adverse
                if (!LineIntersectsOpponentLines(line, game))
                {
                    game.AddScoredLine(line);
                }
            }
        }
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
        int marginLeft, int marginTop, int cellSize,
        int gridWidth, int gridHeight,
        int tolerance)
    {
        int relativeX = clickX - marginLeft;
        int relativeY = clickY - marginTop;

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

    /// <summary>
    /// Fire the current player's cannon and advance the turn.
    /// </summary>
    public CannonFireResult? FireCannon(Game game, int speed)
    {
        if (game.IsFinished)
            return null;

        var cannon = game.CurrentPlayer.Cannon;
        if (cannon == null)
            return null;

        var result = _cannonService.Fire(game, cannon, speed);
        _cannonService.ApplyFireResult(game, result);

        cannon.HasFiredThisTurn = true;
        game.NextTurn();

        return result;
    }
}
