namespace app.model;

public class GameSave
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int BoardWidth { get; set; }
    public int BoardHeight { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsFinished { get; set; }
    public int? WinnerId { get; set; }
    public bool AutoSave { get; set; }
    public int TurnCount { get; set; }

    public GameSave() { }

    public GameSave(string name, int boardWidth, int boardHeight, bool autoSave = false)
    {
        Name = name;
        BoardWidth = boardWidth;
        BoardHeight = boardHeight;
        AutoSave = autoSave;
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }
}
