namespace app.model;

public class Board
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public Point[,] Grid { get; private set; }

    public Board(int width, int height)
    {
        Width = width;
        Height = height;
        Grid = new Point[width, height];
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Grid[x, y] = new Point(x, y);
            }
        }
    }

    public void Reset()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Grid[x, y].Owner = null;
                Grid[x, y].IsPartOfScoredLine = false;
            }
        }
    }

    public Point? GetPoint(int x, int y)
    {
        if (IsValidPosition(x, y))
        {
            return Grid[x, y];
        }
        return null;
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public bool PlacePoint(int x, int y, Player player)
    {
        if (!IsValidPosition(x, y))
            return false;

        if (!Grid[x, y].IsEmpty)
            return false;

        Grid[x, y].Owner = player;
        return true;
    }

    public List<Point> GetAllPlacedPoints()
    {
        var placedPoints = new List<Point>();
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (!Grid[x, y].IsEmpty)
                {
                    placedPoints.Add(Grid[x, y]);
                }
            }
        }
        return placedPoints;
    }
}
