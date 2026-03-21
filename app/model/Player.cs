namespace app.model;

using System.Drawing;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Color Color { get; set; }

    public Player(int id, string name, Color color)
    {
        Id = id;
        Name = name;
        Color = color;
    }
}
