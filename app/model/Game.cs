namespace app.model;

public class Game
{
    public Board Board { get; private set; }
    public List<Player> Players { get; private set; }
    public Player CurrentPlayer { get; private set; }
    public bool IsFinished { get; set; }
    public Player? Winner { get; set; }

    public Game(int boardWidth, int boardHeight, List<Player> players)
    {
        Board = new Board(boardWidth, boardHeight);
        Players = players;
        CurrentPlayer = players[0];
        IsFinished = false;
        Winner = null;
    }

    public void NextTurn()
    {
        int currentIndex = Players.IndexOf(CurrentPlayer);
        int nextIndex = (currentIndex + 1) % Players.Count;
        CurrentPlayer = Players[nextIndex];
    }

    public void Reset()
    {
        Board.Reset();
        CurrentPlayer = Players[0];
        IsFinished = false;
        Winner = null;
    }

    public void Resize(int width, int height)
    {
        Board = new Board(width, height);
        Reset();
    }
}
