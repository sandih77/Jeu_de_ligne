namespace app.model;

public class Game
{
    public Board Board { get; private set; }
    public List<Player> Players { get; private set; }
    public Player CurrentPlayer { get; private set; }
    public bool IsFinished { get; set; }
    public Player? Winner { get; set; }
    public List<Line> ScoredLines { get; private set; }
    public bool CurrentPlayerHasDraggedCannon { get; set; }

    public Dictionary<(int x, int y), Player> DestroyedPoints { get; private set; }

    public Game(int boardWidth, int boardHeight, List<Player> players, bool skipCannonInit = false)
    {
        Board = new Board(boardWidth, boardHeight);
        Players = players;
        CurrentPlayer = players[0];
        IsFinished = false;
        Winner = null;
        ScoredLines = new List<Line>();
        CurrentPlayerHasDraggedCannon = false;
        DestroyedPoints = new Dictionary<(int x, int y), Player>();

        if (!skipCannonInit && players.Count >= 2)
        {
            players[0].InitializeCannon(CannonSide.Left, boardHeight);
            players[1].InitializeCannon(CannonSide.Right, boardHeight);
        }
    }

    public void NextTurn()
    {
        int currentIndex = Players.IndexOf(CurrentPlayer);
        int nextIndex = (currentIndex + 1) % Players.Count;
        CurrentPlayer = Players[nextIndex];
        CurrentPlayerHasDraggedCannon = false;
        CurrentPlayer.Cannon?.ResetForNewTurn();
    }

    public void Reset()
    {
        Board.Reset();
        CurrentPlayer = Players[0];
        IsFinished = false;
        Winner = null;
        ScoredLines.Clear();
        DestroyedPoints.Clear();
        CurrentPlayerHasDraggedCannon = false;
        foreach (var player in Players)
        {
            player.ResetScore();
            player.Cannon?.Reset(Board.Height);
        }
    }

    public void Resize(int width, int height)
    {
        Board = new Board(width, height);
        foreach (var player in Players)
        {
            player.Cannon?.Reset(height);
        }
        Reset();
    }

    public void AddScoredLine(Line line)
    {
        ScoredLines.Add(line);
        line.Owner.AddPoint();
        line.MarkAsScored();
    }
}
