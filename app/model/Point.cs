namespace app.model;

public class Point
{
    public int X { get; set; }
    public int Y { get; set; }
    public Player? Owner { get; set; }
    public bool IsPartOfScoredLine { get; set; }

    public Point(int x, int y, Player? owner = null)
    {
        X = x;
        Y = y;
        Owner = owner;
        IsPartOfScoredLine = false;
    }

    public bool IsEmpty => Owner == null;

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
