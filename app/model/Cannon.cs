namespace app.model;

public enum CannonSide
{
    Left,   // Joueur 1 - tire vers la droite
    Right   // Joueur 2 - tire vers la gauche
}

public class Cannon
{
    public Player Owner { get; private set; }
    public CannonSide Side { get; private set; }
    public int PositionY { get; set; }
    public bool IsDragging { get; set; }
    public bool HasFiredThisTurn { get; set; }

    public Cannon(Player owner, CannonSide side, int initialY)
    {
        Owner = owner;
        Side = side;
        PositionY = initialY;
        IsDragging = false;
        HasFiredThisTurn = false;
    }

    public void ResetForNewTurn()
    {
        HasFiredThisTurn = false;
        IsDragging = false;
    }

    public void Reset(int boardHeight)
    {
        PositionY = boardHeight / 2;
        HasFiredThisTurn = false;
        IsDragging = false;
    }
}
