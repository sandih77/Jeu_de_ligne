namespace app.model;

public class CannonFireResult
{
    public Cannon Cannon { get; set; }
    public int Speed { get; set; }
    public int StartX { get; set; }
    public int TargetX { get; set; }
    public int Y { get; set; }
    public Point? HitPoint { get; set; }
    public bool WasDestroyed { get; set; }
    public bool HitInvulnerable { get; set; }

    public CannonFireResult(Cannon cannon, int speed, int y)
    {
        Cannon = cannon;
        Speed = speed;
        Y = y;
        StartX = cannon.Side == CannonSide.Left ? 0 : 0;
        TargetX = 0;
        HitPoint = null;
        WasDestroyed = false;
        HitInvulnerable = false;
    }
}
