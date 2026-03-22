namespace app.model;

public class TurnState
{
    public int Id { get; set; }
    public int GameSaveId { get; set; }
    public int TurnNumber { get; set; }
    public int CurrentPlayerId { get; set; }
    public DateTime SavedAt { get; set; }
    public string ActionType { get; set; } = "";
    public string? Description { get; set; }

    // Donnees deserializees
    public List<PointState> Points { get; set; } = new();
    public List<LineState> Lines { get; set; } = new();
    public Dictionary<int, int> PlayerScores { get; set; } = new();

    public TurnState() { }

    public TurnState(int gameSaveId, int turnNumber, int currentPlayerId, string actionType, string? description = null)
    {
        GameSaveId = gameSaveId;
        TurnNumber = turnNumber;
        CurrentPlayerId = currentPlayerId;
        ActionType = actionType;
        Description = description;
        SavedAt = DateTime.Now;
    }
}

public class PointState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int OwnerPlayerId { get; set; }
    public bool IsPartOfScoredLine { get; set; }
    public List<(int dx, int dy)> ScoredDirections { get; set; } = new();
}

public class LineState
{
    public int OwnerPlayerId { get; set; }
    public bool IsScored { get; set; }
    public List<(int x, int y)> Points { get; set; } = new();
}
