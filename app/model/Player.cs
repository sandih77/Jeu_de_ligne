namespace app.model;

using System.Drawing;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Color Color { get; set; }
    public int Score { get; set; }
    public Cannon? Cannon { get; set; }

    public Player(int id, string name, Color color)
    {
        Id = id;
        Name = name;
        Color = color;
        Score = 0;
        Cannon = null;
    }

    public void InitializeCannon(CannonSide side, int boardHeight)
    {
        Cannon = new Cannon(this, side, boardHeight / 2);
    }

    public void AddPoint()
    {
        Score++;
    }

    public void ResetScore()
    {
        Score = 0;
    }
}
