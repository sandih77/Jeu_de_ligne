namespace app.view;

using app.model;
using app.controller;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class PlateauView : Panel
{
    private readonly GameController _controller;
    private int _marginTop = 80;    // Marge en haut pour vitesses canon gauche
    private int _marginBottom = 80; // Marge en bas pour vitesses canon droit
    private int _marginLeft = 180;  // Marge à gauche (canon gauche décalé)
    private int _marginRight = 180; // Marge à droite (canon droit décalé)
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

    public int MarginLeft => _marginLeft;
    public int MarginTop => _marginTop;
    public int CellSize => _cellSize;

    public PlateauView(GameController controller)
    {
        _controller = controller;

        this.DoubleBuffered = true;
        this.BackColor = Color.FromArgb(245, 247, 250); // Gris-bleu moderne
        this.BorderStyle = BorderStyle.None;

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
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        var board = _controller.Board;
        CalculateCellSize(board.Width, board.Height);

        DrawBackground(g);
        DrawSpeedIndicators(g, board);
        DrawGrid(g, board);
        DrawScoredLines(g);
        DrawPoints(g, board);
        DrawCannons(g);
        DrawFireAnimation(g);
        DrawExplosionEffect(g);
    }

    private void DrawBackground(Graphics g)
    {
        // Dégradé moderne plus sophistiqué
        using (var brush = new LinearGradientBrush(
            this.ClientRectangle,
            Color.FromArgb(245, 247, 250),
            Color.FromArgb(225, 230, 240),
            LinearGradientMode.Vertical))
        {
            g.FillRectangle(brush, this.ClientRectangle);
        }

        // Bordure décorative en haut
        using (var borderBrush = new LinearGradientBrush(
            new Rectangle(0, 0, this.Width, 3),
            Color.FromArgb(100, 120, 200),
            Color.FromArgb(80, 180, 220),
            LinearGradientMode.Horizontal))
        {
            g.FillRectangle(borderBrush, 0, 0, this.Width, 3);
        }
    }

    private void DrawSpeedIndicators(Graphics g, Board board)
    {
        using var font = new Font("Segoe UI", 11, FontStyle.Bold);

        var leftCannonColor = _controller.Player1Cannon?.Owner.Color ?? Color.Blue;
        var rightCannonColor = _controller.Player2Cannon?.Owner.Color ?? Color.Red;

        // Suivre les colonnes déjà utilisées pour gérer les superpositions
        Dictionary<int, int> leftColumnCount = new Dictionary<int, int>();
        Dictionary<int, int> rightColumnCount = new Dictionary<int, int>();

        // Indicateurs pour le canon GAUCHE (en haut) - sans titre
        for (int speed = 1; speed <= 9; speed++)
        {
            int targetX = CalculateTargetColumn(CannonSide.Left, speed, board.Width);
            int xPos = _marginLeft + targetX * _cellSize;

            // Décalage si plusieurs vitesses sur la même colonne
            if (!leftColumnCount.ContainsKey(targetX))
                leftColumnCount[targetX] = 0;

            int offset = leftColumnCount[targetX] * 36; // Décalage horizontal de 36px par indicateur
            leftColumnCount[targetX]++;

            int indicatorX = xPos - 16 + offset;

            // Ligne de guidage plus visible (seulement pour le premier indicateur de chaque colonne)
            if (leftColumnCount[targetX] == 1)
            {
                using (var pen = new Pen(Color.FromArgb(140, leftCannonColor), 3))
                {
                    pen.DashStyle = DashStyle.Dot;
                    pen.DashPattern = new float[] { 2, 4 };
                    g.DrawLine(pen, xPos, 25, xPos, _marginTop - 5);
                }
            }

            Rectangle speedRect = new Rectangle(indicatorX, 25, 32, 34);

            // Ombre de l'indicateur
            Rectangle shadowRect = new Rectangle(speedRect.X + 2, speedRect.Y + 2, speedRect.Width, speedRect.Height);
            using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            {
                g.FillRoundedRectangle(shadowBrush, shadowRect, 6);
            }

            // Fond avec dégradé moderne
            using (var brush = new LinearGradientBrush(speedRect,
                Color.FromArgb(255, leftCannonColor),
                Color.FromArgb(180, leftCannonColor),
                LinearGradientMode.Vertical))
            {
                g.FillRoundedRectangle(brush, speedRect, 6);
            }

            // Bordure
            using (var borderPen = new Pen(Color.FromArgb(150, 0, 0, 0), 2))
            {
                g.DrawRoundedRectangle(borderPen, speedRect, 6);
            }

            // Reflet lumineux
            Rectangle highlightRect = new Rectangle(speedRect.X + 3, speedRect.Y + 3, speedRect.Width - 6, speedRect.Height / 3);
            using (var highlightBrush = new LinearGradientBrush(highlightRect,
                Color.FromArgb(70, Color.White),
                Color.FromArgb(0, Color.White),
                LinearGradientMode.Vertical))
            {
                g.FillRoundedRectangle(highlightBrush, highlightRect, 4);
            }

            // Numéro de vitesse
            var speedStr = speed.ToString();
            var strSize = g.MeasureString(speedStr, font);
            g.DrawString(speedStr, font, Brushes.White, indicatorX + 16 - strSize.Width / 2, 30);
        }

        // Indicateurs pour le canon DROIT (en bas) - sans titre
        int bottomY = _marginTop + (board.Height - 1) * _cellSize + _marginBottom - 35;

        for (int speed = 1; speed <= 9; speed++)
        {
            int targetX = CalculateTargetColumn(CannonSide.Right, speed, board.Width);
            int xPos = _marginLeft + targetX * _cellSize;

            // Décalage si plusieurs vitesses sur la même colonne
            if (!rightColumnCount.ContainsKey(targetX))
                rightColumnCount[targetX] = 0;

            int offset = rightColumnCount[targetX] * 36; // Décalage horizontal de 36px par indicateur
            rightColumnCount[targetX]++;

            int indicatorX = xPos - 16 + offset;

            int gridBottom = _marginTop + (board.Height - 1) * _cellSize;

            // Ligne de guidage plus visible (seulement pour le premier indicateur de chaque colonne)
            if (rightColumnCount[targetX] == 1)
            {
                using (var pen = new Pen(Color.FromArgb(140, rightCannonColor), 3))
                {
                    pen.DashStyle = DashStyle.Dot;
                    pen.DashPattern = new float[] { 2, 4 };
                    g.DrawLine(pen, xPos, gridBottom + 5, xPos, bottomY);
                }
            }

            Rectangle speedRect = new Rectangle(indicatorX, bottomY, 32, 34);

            // Ombre de l'indicateur
            Rectangle shadowRect = new Rectangle(speedRect.X + 2, speedRect.Y + 2, speedRect.Width, speedRect.Height);
            using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            {
                g.FillRoundedRectangle(shadowBrush, shadowRect, 6);
            }

            // Fond avec dégradé moderne
            using (var brush = new LinearGradientBrush(speedRect,
                Color.FromArgb(255, rightCannonColor),
                Color.FromArgb(180, rightCannonColor),
                LinearGradientMode.Vertical))
            {
                g.FillRoundedRectangle(brush, speedRect, 6);
            }

            // Bordure
            using (var borderPen = new Pen(Color.FromArgb(150, 0, 0, 0), 2))
            {
                g.DrawRoundedRectangle(borderPen, speedRect, 6);
            }

            // Reflet lumineux
            Rectangle highlightRect = new Rectangle(speedRect.X + 3, speedRect.Y + 3, speedRect.Width - 6, speedRect.Height / 3);
            using (var highlightBrush = new LinearGradientBrush(highlightRect,
                Color.FromArgb(70, Color.White),
                Color.FromArgb(0, Color.White),
                LinearGradientMode.Vertical))
            {
                g.FillRoundedRectangle(highlightBrush, highlightRect, 4);
            }

            // Numéro de vitesse
            var speedStr = speed.ToString();
            var strSize = g.MeasureString(speedStr, font);
            g.DrawString(speedStr, font, Brushes.White, indicatorX + 16 - strSize.Width / 2, bottomY + 6);
        }
    }

    private int CalculateTargetColumn(CannonSide side, int speed, int boardWidth)
    {
        double proportion = speed / 9.0;
        int distance = (int)Math.Floor(boardWidth * proportion) - 1;

        if (side == CannonSide.Left)
        {
            // Tire depuis la gauche (x=0) vers la droite
            return Math.Min(distance, boardWidth - 1);
        }
        else
        {
            // Tire depuis la droite (x=boardWidth-1) vers la gauche
            return Math.Max(boardWidth - 1 - distance, 0);
        }
    }

    private void CalculateCellSize(int gridWidth, int gridHeight)
    {
        int availableWidth = this.Width - _marginLeft - _marginRight;
        int availableHeight = this.Height - _marginTop - _marginBottom;

        if (gridWidth > 1 && gridHeight > 1)
        {
            _cellSize = Math.Min(
                availableWidth / (gridWidth - 1),
                availableHeight / (gridHeight - 1));
        }
    }

    private void DrawGrid(Graphics g, Board board)
    {
        // Cadre avec bordure arrondie autour de la grille
        int gridWidth = (board.Width - 1) * _cellSize;
        int gridHeight = (board.Height - 1) * _cellSize;
        Rectangle gridBounds = new Rectangle(_marginLeft - 10, _marginTop - 10, gridWidth + 20, gridHeight + 20);

        // Ombre du cadre
        Rectangle shadowBounds = new Rectangle(gridBounds.X + 4, gridBounds.Y + 4, gridBounds.Width, gridBounds.Height);
        using (var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
        {
            g.FillRoundedRectangle(shadowBrush, shadowBounds, 12);
        }

        // Fond du cadre
        using (var bgBrush = new LinearGradientBrush(gridBounds,
            Color.FromArgb(255, 255, 255),
            Color.FromArgb(248, 250, 252),
            LinearGradientMode.Vertical))
        {
            g.FillRoundedRectangle(bgBrush, gridBounds, 12);
        }

        // Bordure du cadre
        using (var borderPen = new Pen(Color.FromArgb(180, 190, 200), 2))
        {
            g.DrawRoundedRectangle(borderPen, gridBounds, 12);
        }

        // Ombres des lignes de grille
        using (var shadowPen = new Pen(Color.FromArgb(20, 0, 0, 0), 2))
        {
            for (int x = 0; x < board.Width; x++)
            {
                int xPos = _marginLeft + x * _cellSize + 1;
                g.DrawLine(shadowPen, xPos, _marginTop + 1, xPos, _marginTop + gridHeight + 1);
            }
            for (int y = 0; y < board.Height; y++)
            {
                int yPos = _marginTop + y * _cellSize + 1;
                g.DrawLine(shadowPen, _marginLeft + 1, yPos, _marginLeft + gridWidth + 1, yPos);
            }
        }

        // Lignes de grille principales
        using var linePen = new Pen(Color.FromArgb(150, 160, 170), 1.5f);
        for (int x = 0; x < board.Width; x++)
        {
            int xPos = _marginLeft + x * _cellSize;
            g.DrawLine(linePen, xPos, _marginTop, xPos, _marginTop + gridHeight);
        }
        for (int y = 0; y < board.Height; y++)
        {
            int yPos = _marginTop + y * _cellSize;
            g.DrawLine(linePen, _marginLeft, yPos, _marginLeft + gridWidth, yPos);
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

                int x1 = _marginLeft + firstPoint.X * _cellSize;
                int y1 = _marginTop + firstPoint.Y * _cellSize;
                int x2 = _marginLeft + lastPoint.X * _cellSize;
                int y2 = _marginTop + lastPoint.Y * _cellSize;

                // Effet de lueur externe
                using (var glowPen = new Pen(Color.FromArgb(60, line.Owner.Color), 12))
                {
                    glowPen.StartCap = LineCap.Round;
                    glowPen.EndCap = LineCap.Round;
                    g.DrawLine(glowPen, x1, y1, x2, y2);
                }

                // Ombre portée
                using (var shadowPen = new Pen(Color.FromArgb(70, 0, 0, 0), 7))
                {
                    shadowPen.StartCap = LineCap.Round;
                    shadowPen.EndCap = LineCap.Round;
                    g.DrawLine(shadowPen, x1 + 2, y1 + 2, x2 + 2, y2 + 2);
                }

                // Ligne principale avec couleur vive
                using var linePen = new Pen(line.Owner.Color, 5);
                linePen.DashStyle = DashStyle.Solid;
                linePen.StartCap = LineCap.Round;
                linePen.EndCap = LineCap.Round;
                g.DrawLine(linePen, x1, y1, x2, y2);

                // Ligne de reflet blanche pour effet 3D
                using var highlightPen = new Pen(Color.FromArgb(80, Color.White), 3);
                highlightPen.StartCap = LineCap.Round;
                highlightPen.EndCap = LineCap.Round;

                // Calculer une position légèrement décalée pour le reflet
                double angle = Math.Atan2(y2 - y1, x2 - x1);
                int offsetX = (int)(2 * Math.Sin(angle));
                int offsetY = -(int)(2 * Math.Cos(angle));

                g.DrawLine(highlightPen, x1 + offsetX, y1 + offsetY, x2 + offsetX, y2 + offsetY);
            }
        }
    }

    private void DrawPoints(Graphics g, Board board)
    {
        int pointRadius = Math.Max(8, _cellSize / 4);

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                var point = board.GetPoint(x, y);
                if (point != null && !point.IsEmpty)
                {
                    int xPos = _marginLeft + x * _cellSize;
                    int yPos = _marginTop + y * _cellSize;

                    // Ombre portée plus prononcée
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                    {
                        g.FillEllipse(shadowBrush, xPos - pointRadius + 3, yPos - pointRadius + 3, pointRadius * 2, pointRadius * 2);
                    }

                    Rectangle pointRect = new Rectangle(xPos - pointRadius, yPos - pointRadius, pointRadius * 2, pointRadius * 2);

                    // Dégradé radial pour un effet 3D
                    using (var path = new GraphicsPath())
                    {
                        path.AddEllipse(pointRect);
                        using (var brush = new PathGradientBrush(path))
                        {
                            // Couleur centrale plus claire pour effet de brillance
                            brush.CenterColor = Color.FromArgb(
                                Math.Min(255, point.Owner!.Color.R + 80),
                                Math.Min(255, point.Owner!.Color.G + 80),
                                Math.Min(255, point.Owner!.Color.B + 80));
                            brush.SurroundColors = new[] { point.Owner!.Color };

                            // Ajuster le point central pour l'effet de lumière
                            brush.CenterPoint = new PointF(xPos - pointRadius / 4, yPos - pointRadius / 4);
                            g.FillEllipse(brush, pointRect);
                        }
                    }

                    // Bordure et effets selon l'état
                    if (point.IsPartOfScoredLine)
                    {
                        // Effet de halo doré pour les points scorés
                        using var glowPen = new Pen(Color.FromArgb(100, Color.Gold), 6);
                        g.DrawEllipse(glowPen,
                            xPos - pointRadius - 2, yPos - pointRadius - 2,
                            pointRadius * 2 + 4, pointRadius * 2 + 4);

                        using var borderPen = new Pen(Color.Gold, 3);
                        g.DrawEllipse(borderPen, pointRect);
                    }
                    else
                    {
                        // Bordure normale avec effet de profondeur
                        using var borderPen = new Pen(Color.FromArgb(50, 50, 50), 2);
                        g.DrawEllipse(borderPen, pointRect);

                        // Reflet blanc subtil
                        int highlightSize = pointRadius / 2;
                        using var highlightBrush = new SolidBrush(Color.FromArgb(100, Color.White));
                        g.FillEllipse(highlightBrush,
                            xPos - pointRadius / 2, yPos - pointRadius / 2,
                            highlightSize, highlightSize);
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
            int y = _marginTop + cannon1.PositionY * _cellSize;
            int x = _marginLeft - 50; // Décalé plus loin de la grille
            bool isActive = cannon1 == currentCannon;
            bool isDragging = cannon1.IsDragging;
            CannonRenderer.DrawCannon(g, cannon1, x, y, isActive, isDragging);

            if (isDragging)
            {
                int gridRight = _marginLeft + (_controller.Board.Width - 1) * _cellSize;
                CannonRenderer.DrawAimLine(g, x + CannonRenderer.BarrelLength, y, gridRight, cannon1.Owner.Color);
            }
        }

        if (cannon2 != null)
        {
            int y = _marginTop + cannon2.PositionY * _cellSize;
            int x = _marginLeft + (_controller.Board.Width - 1) * _cellSize + 50; // Décalé plus loin de la grille
            bool isActive = cannon2 == currentCannon;
            bool isDragging = cannon2.IsDragging;
            CannonRenderer.DrawCannon(g, cannon2, x, y, isActive, isDragging);

            if (isDragging)
            {
                CannonRenderer.DrawAimLine(g, x - CannonRenderer.BarrelLength, y, _marginLeft, cannon2.Owner.Color);
            }
        }
    }

    private void StartFireAnimation(CannonFireResult result)
    {
        _currentFireAnimation = result;
        if (result.Cannon.Side == CannonSide.Left)
            _projectileAnimationX = _marginLeft - 50 + CannonRenderer.BarrelLength; // Position du bout du canon gauche
        else
            _projectileAnimationX = _marginLeft + (_controller.Board.Width - 1) * _cellSize + 50 - CannonRenderer.BarrelLength; // Position du bout du canon droit

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

        int targetPixelX = _marginLeft + _currentFireAnimation.TargetX * _cellSize;
        int direction = _currentFireAnimation.Cannon.Side == CannonSide.Left ? 1 : -1;
        int speed = 20;

        _projectileAnimationX += direction * speed;

        bool reachedTarget = _currentFireAnimation.Cannon.Side == CannonSide.Left
            ? _projectileAnimationX >= targetPixelX
            : _projectileAnimationX <= targetPixelX;

        if (reachedTarget)
            OnFireAnimationComplete();

        this.Invalidate();
    }

    private void DrawFireAnimation(Graphics g)
    {
        if (_currentFireAnimation == null) return;
        int y = _marginTop + _currentFireAnimation.Y * _cellSize;
        CannonRenderer.DrawProjectile(g, _projectileAnimationX, y, _currentFireAnimation.Cannon.Owner.Color);
    }

    private void OnFireAnimationComplete()
    {
        if (_currentFireAnimation != null && _currentFireAnimation.HitPoint != null)
        {
            _explosionX = _marginLeft + _currentFireAnimation.HitPoint.X * _cellSize;
            _explosionY = _marginTop + _currentFireAnimation.HitPoint.Y * _cellSize;
            _hitWasInvulnerable = _currentFireAnimation.HitInvulnerable;

            _explosionColor = _currentFireAnimation.WasDestroyed
                ? _currentFireAnimation.HitPoint.Owner?.Color ?? Color.Red
                : Color.Gold;

            _showingExplosion = true;
            _explosionFrame = 0;
        }

        _currentFireAnimation = null;
    }

    private void DrawExplosionEffect(Graphics g)
    {
        if (!_showingExplosion) return;

        if (_hitWasInvulnerable)
            CannonRenderer.DrawShieldEffect(g, _explosionX, _explosionY, _explosionFrame);
        else
            CannonRenderer.DrawExplosion(g, _explosionX, _explosionY, _explosionFrame, _explosionColor);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (_controller.HasDraggedCannonThisTurn) return;
        if (IsClickOnCurrentPlayerCannon(e.X, e.Y)) return;

        _controller.HandleClick(e.X, e.Y, _marginLeft, _marginTop, _cellSize);
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
            int gridY = (e.Y - _marginTop + _cellSize / 2) / _cellSize;
            gridY = Math.Clamp(gridY, 0, _controller.Board.Height - 1);
            _controller.MoveCannonTo(gridY);
        }
        else
        {
            this.Cursor = IsClickOnCurrentPlayerCannon(e.X, e.Y) ? Cursors.Hand : Cursors.Default;
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
        if (cannon == null) return false;

        int cannonY = _marginTop + cannon.PositionY * _cellSize;

        if (cannon.Side == CannonSide.Left)
            return x < _marginLeft && Math.Abs(y - cannonY) < CannonRenderer.CannonHeight + 10;
        else
        {
            int gridRight = _marginLeft + (_controller.Board.Width - 1) * _cellSize;
            return x > gridRight && Math.Abs(y - cannonY) < CannonRenderer.CannonHeight + 10;
        }
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        this.Invalidate();
    }
}