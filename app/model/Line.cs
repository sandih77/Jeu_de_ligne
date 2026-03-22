namespace app.model;

public class Line
{
    public List<Point> Points { get; set; }
    public Player Owner { get; set; }
    public bool IsScored { get; set; }

    public Line(List<Point> points, Player owner)
    {
        Points = points;
        Owner = owner;
        IsScored = false;
    }

    public void MarkAsScored()
    {
        IsScored = true;
    }
}
