namespace app.view;

using app.model;
using app.controller;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class PlateauView : Panel
{
    private readonly GameController _controller;
    private int _margin = 70; // Augmente pour laisser place aux canons
    private int _cellSize = 40;

    // Cannon interaction state
    private bool _isDraggingCannon = false;
    private CannonFireResult? _currentFireAnimation = null;
    private System.Windows.Forms.Timer? _animationTimer;
    private int _projectileAnimationX;
    private int _explosionFrame = 0;
    private bool _showingExplosion = false;
    private int _explosionX, _explosionY;
    private Color _explosionColor;
    private bool _hitWasInvulnerable = false;

    public int Margin_ => _margin;
    public int CellSize => _cellSize;

    public PlateauView(GameController controller)
    {
        _controller = controller;

        this.DoubleBuffered = true;
        this.BackColor = Color.FromArgb(245, 235, 220); // Beige plus doux
        this.BorderStyle = BorderStyle.FixedSingle;

        _controller.OnGameUpdated += () => this.Invalidate();
        _controller.OnCannonFired += StartFireAnimation;
        _controller.OnCannonMoved += (c) => this.Invalidate();

        // Timer pour l'animation
        _animationTimer = new System.Windows.Forms.Timer();
        _animationTimer.Interval = 16; // ~60fps
        _animationTimer.Tick += AnimationTimer_Tick;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        DrawBoard(e.Graphics);
    }

    private void DrawBoard(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var board = _controller.Board;
        CalculateCellSize(board.Width, board.Height);

        DrawBackground(g);
        DrawGrid(g, board);
        DrawScoredLines(g);
        DrawPoints(g, board);
        DrawCannons(g);
        DrawFireAnimation(g);
        DrawExplosionEffect(g);
    }

    private void DrawBackground(Graphics g)
    {
        // Fond avec leger degrade
        using (var brush = new LinearGradientBrush(
            this.ClientRectangle,
            Color.FromArgb(250, 245, 235),
            Color.FromArgb(235, 225, 210),
            LinearGradientMode.Vertical))
        {
            g.FillRectangle(brush, this.ClientRectangle);
        }
    }

    private void CalculateCellSize(int gridWidth, int gridHeight)
    {
        int availableWidth = this.Width - 2 * _margin;
        int availableHeight = this.Height - 2 * _margin;

        if (gridWidth > 1 && gridHeight > 1)
        {
            _cellSize = Math.Min(
                availableWidth / (gridWidth - 1),
                availableHeight / (gridHeight - 1));
        }
    }

    private void DrawGrid(Graphics g, Board board)
    {
        // Ombre de la grille
        using (var shadowPen = new Pen(Color.FromArgb(30, 0, 0, 0), 3))
        {
            for (int x = 0; x < board.Width; x++)
            {
                int xPos = _margin + x * _cellSize + 2;
                g.DrawLine(shadowPen, xPos, _margin + 2, xPos, _margin + (board.Height - 1) * _cellSize + 2);
            }
            for (int y = 0; y < board.Height; y++)
            {
                int yPos = _margin + y * _cellSize + 2;
                g.DrawLine(shadowPen, _margin + 2, yPos, _margin + (board.Width - 1) * _cellSize + 2, yPos);
            }
        }

        // Grille principale
        using var linePen = new Pen(Color.FromArgb(60, 50, 40), 2);

        for (int x = 0; x < board.Width; x++)
        {
            int xPos = _margin + x * _cellSize;
            g.DrawLine(linePen, xPos, _margin, xPos, _margin + (board.Height - 1) * _cellSize);
        }

        for (int y = 0; y < board.Height; y++)
        {
            int yPos = _margin + y * _cellSize;
            g.DrawLine(linePen, _margin, yPos, _margin + (board.Width - 1) * _cellSize, yPos);
        }
    }

    private void DrawScoredLines(Graphics g)
    {
        var scoredLines = _controller.ScoredLines;

        foreach (var line in scoredLines)
        {
            if (line.Points.Count >= 2)
            {
                var firstPoint = line.Points.First();
                var lastPoint = line.Points.Last();

                int x1 = _margin + firstPoint.X * _cellSize;
                int y1 = _margin + firstPoint.Y * _cellSize;
                int x2 = _margin + lastPoint.X * _cellSize;
                int y2 = _margin + lastPoint.Y * _cellSize;

                // Ombre de la ligne
                using (var shadowPen = new Pen(Color.FromArgb(50, 0, 0, 0), 6))
                {
                    shadowPen.StartCap = LineCap.Round;
                    shadowPen.EndCap = LineCap.Round;
                    g.DrawLine(shadowPen, x1 + 2, y1 + 2, x2 + 2, y2 + 2);
                }

                // Ligne principale
                using var linePen = new Pen(line.Owner.Color, 4);
                linePen.DashStyle = DashStyle.Solid;
                linePen.StartCap = LineCap.Round;
                linePen.EndCap = LineCap.Round;

                g.DrawLine(linePen, x1, y1, x2, y2);
            }
        }
    }

    private void DrawPoints(Graphics g, Board board)
    {
        int pointRadius = _cellSize / 4;

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                var point = board.GetPoint(x, y);
                if (point != null && !point.IsEmpty)
                {
                    int xPos = _margin + x * _cellSize;
                    int yPos = _margin + y * _cellSize;

                    // Ombre du point
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                    {
                        g.FillEllipse(shadowBrush,
                            xPos - pointRadius + 2,
                            yPos - pointRadius + 2,
                            pointRadius * 2,
                            pointRadius * 2);
                    }

                    // Gradient pour le point
                    Rectangle pointRect = new Rectangle(
                        xPos - pointRadius, yPos - pointRadius,
                        pointRadius * 2, pointRadius * 2);

                    using (var path = new GraphicsPath())
                    {
                        path.AddEllipse(pointRect);
                        using (var brush = new PathGradientBrush(path))
                        {
                            brush.CenterColor = Color.FromArgb(
                                Math.Min(255, point.Owner!.Color.R + 60),
                                Math.Min(255, point.Owner!.Color.G + 60),
                                Math.Min(255, point.Owner!.Color.B + 60));
                            brush.SurroundColors = new[] { point.Owner!.Color };
                            g.FillEllipse(brush, pointRect);
                        }
                    }

                    // Bordure speciale pour les points faisant partie d'une ligne scoree
                    if (point.IsPartOfScoredLine)
                    {
                        using var borderPen = new Pen(Color.Gold, 3);
                        g.DrawEllipse(borderPen, pointRect);
                        // Effet de brillance
                        using var glowPen = new Pen(Color.FromArgb(80, Color.Gold), 5);
                        g.DrawEllipse(glowPen, pointRect);
                    }
                    else
                    {
                        g.DrawEllipse(Pens.Black, pointRect);
                    }
                }
            }
        }
    }

    private void DrawCannons(Graphics g)
    {
        var cannon1 = _controller.Player1Cannon;
        var cannon2 = _controller.Player2Cannon;
        var currentCannon = _controller.CurrentPlayerCannon;

        if (cannon1 != null)
        {
            int y = _margin + cannon1.PositionY * _cellSize;
            int x = _margin - 15;
            bool isActive = cannon1 == currentCannon;
            bool isDragging = cannon1.IsDragging;
            CannonRenderer.DrawCannon(g, cannon1, x, y, isActive, isDragging);

            // Ligne de visee quand on drag
            if (isDragging)
            {
                int gridRight = _margin + (_controller.Board.Width - 1) * _cellSize;
                CannonRenderer.DrawAimLine(g, x + CannonRenderer.BarrelLength, y, gridRight, cannon1.Owner.Color);
            }
        }

        if (cannon2 != null)
        {
            int y = _margin + cannon2.PositionY * _cellSize;
            int x = _margin + (_controller.Board.Width - 1) * _cellSize + 15;
            bool isActive = cannon2 == currentCannon;
            bool isDragging = cannon2.IsDragging;
            CannonRenderer.DrawCannon(g, cannon2, x, y, isActive, isDragging);

            // Ligne de visee quand on drag
            if (isDragging)
            {
                CannonRenderer.DrawAimLine(g, x - CannonRenderer.BarrelLength, y, _margin, cannon2.Owner.Color);
            }
        }
    }

    // ============ ANIMATION ============

    private void StartFireAnimation(CannonFireResult result)
    {
        _currentFireAnimation = result;
        if (result.Cannon.Side == CannonSide.Left)
        {
            _projectileAnimationX = _margin;
        }
        else
        {
            _projectileAnimationX = _margin + (_controller.Board.Width - 1) * _cellSize;
        }
        _animationTimer?.Start();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (_showingExplosion)
        {
            _explosionFrame++;
            if (_explosionFrame > 10)
            {
                _showingExplosion = false;
                _explosionFrame = 0;
            }
            this.Invalidate();
            return;
        }

        if (_currentFireAnimation == null)
        {
            _animationTimer?.Stop();
            return;
        }

        int targetPixelX = _margin + _currentFireAnimation.TargetX * _cellSize;
        int direction = _currentFireAnimation.Cannon.Side == CannonSide.Left ? 1 : -1;
        int speed = 20; // Pixels par frame

        _projectileAnimationX += direction * speed;

        bool reachedTarget = _currentFireAnimation.Cannon.Side == CannonSide.Left
            ? _projectileAnimationX >= targetPixelX
            : _projectileAnimationX <= targetPixelX;

        if (reachedTarget)
        {
            OnFireAnimationComplete();
        }

        this.Invalidate();
    }

    private void DrawFireAnimation(Graphics g)
    {
        if (_currentFireAnimation == null)
            return;

        int y = _margin + _currentFireAnimation.Y * _cellSize;
        CannonRenderer.DrawProjectile(g, _projectileAnimationX, y, _currentFireAnimation.Cannon.Owner.Color);
    }

    private void OnFireAnimationComplete()
    {
        if (_currentFireAnimation != null)
        {
            if (_currentFireAnimation.HitPoint != null)
            {
                _explosionX = _margin + _currentFireAnimation.HitPoint.X * _cellSize;
                _explosionY = _margin + _currentFireAnimation.HitPoint.Y * _cellSize;
                _hitWasInvulnerable = _currentFireAnimation.HitInvulnerable;

                if (_currentFireAnimation.WasDestroyed)
                {
                    _explosionColor = _currentFireAnimation.HitPoint.Owner?.Color ?? Color.Red;
                }
                else
                {
                    _explosionColor = Color.Gold;
                }

                _showingExplosion = true;
                _explosionFrame = 0;
            }
        }
        _currentFireAnimation = null;
    }

    private void DrawExplosionEffect(Graphics g)
    {
        if (!_showingExplosion)
            return;

        if (_hitWasInvulnerable)
        {
            CannonRenderer.DrawShieldEffect(g, _explosionX, _explosionY, _explosionFrame);
        }
        else
        {
            CannonRenderer.DrawExplosion(g, _explosionX, _explosionY, _explosionFrame, _explosionColor);
        }
    }

    // ============ MOUSE INTERACTION ============

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        // Si le joueur a deja deplace le canon, il ne peut plus placer de points
        if (_controller.HasDraggedCannonThisTurn)
            return;

        // Verifier si le clic est sur la zone du canon
        if (IsClickOnCurrentPlayerCannon(e.X, e.Y))
            return;

        _controller.HandleClick(e.X, e.Y, _margin, _cellSize);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (IsClickOnCurrentPlayerCannon(e.X, e.Y))
        {
            _isDraggingCannon = true;
            _controller.StartCannonDrag();
            this.Cursor = Cursors.SizeNS;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isDraggingCannon)
        {
            int gridY = (e.Y - _margin + _cellSize / 2) / _cellSize;
            gridY = Math.Clamp(gridY, 0, _controller.Board.Height - 1);
            _controller.MoveCannonTo(gridY);
        }
        else
        {
            // Changer le curseur selon la zone
            if (IsClickOnCurrentPlayerCannon(e.X, e.Y))
            {
                this.Cursor = Cursors.Hand;
            }
            else
            {
                this.Cursor = Cursors.Default;
            }
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_isDraggingCannon)
        {
            _isDraggingCannon = false;
            _controller.EndCannonDrag();
            this.Cursor = Cursors.Default;
        }
    }

    private bool IsClickOnCurrentPlayerCannon(int x, int y)
    {
        var cannon = _controller.CurrentPlayerCannon;
        if (cannon == null)
            return false;

        int cannonY = _margin + cannon.PositionY * _cellSize;

        if (cannon.Side == CannonSide.Left)
        {
            return x < _margin &&
                   Math.Abs(y - cannonY) < CannonRenderer.CannonHeight + 10;
        }
        else
        {
            int gridRight = _margin + (_controller.Board.Width - 1) * _cellSize;
            return x > gridRight &&
                   Math.Abs(y - cannonY) < CannonRenderer.CannonHeight + 10;
        }
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        this.Invalidate();
    }
}
