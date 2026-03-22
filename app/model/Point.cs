namespace app.model;

public class Point
{
    public int X { get; set; }
    public int Y { get; set; }
    public Player? Owner { get; set; }
    public HashSet<(int dx, int dy)> ScoredDirections { get; set; }

    public Point(int x, int y, Player? owner = null)
    {
        X = x;
        Y = y;
        Owner = owner;
        ScoredDirections = new HashSet<(int dx, int dy)>();
    }

    public bool IsEmpty => Owner == null;

    public bool IsPartOfScoredLine => ScoredDirections.Count > 0;

    public bool IsScoredInDirection(int dx, int dy)
    {
        return ScoredDirections.Contains((dx, dy));
    }

    public void MarkScoredInDirection(int dx, int dy)
    {
        ScoredDirections.Add((dx, dy));
    }

    public override bool Equals(object? obj)
    {
        if (obj is Point other)
        {
            return X == other.X && Y == other.Y;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}
