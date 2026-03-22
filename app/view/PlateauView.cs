namespace app.view;

using app.model;
using app.controller;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class PlateauView : Panel
{
    private readonly GameController _controller;
    private int _margin = 40;
    private int _cellSize = 40;

    public int Margin_ => _margin;
    public int CellSize => _cellSize;

    public PlateauView(GameController controller)
    {
        _controller = controller;

        this.DoubleBuffered = true;
        this.BackColor = Color.Beige;
        this.BorderStyle = BorderStyle.FixedSingle;

        _controller.OnGameUpdated += () => this.Invalidate();
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

        DrawGrid(g, board);
        DrawScoredLines(g);
        DrawPoints(g, board);
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
        using var linePen = new Pen(Color.Black, 2);

        // Lignes verticales
        for (int x = 0; x < board.Width; x++)
        {
            int xPos = _margin + x * _cellSize;
            g.DrawLine(linePen, xPos, _margin, xPos, _margin + (board.Height - 1) * _cellSize);
        }

        // Lignes horizontales
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

                    using var brush = new SolidBrush(point.Owner!.Color);
                    g.FillEllipse(brush,
                        xPos - pointRadius,
                        yPos - pointRadius,
                        pointRadius * 2,
                        pointRadius * 2);

                    // Bordure spéciale pour les points faisant partie d'une ligne scorée
                    Pen borderPen = point.IsPartOfScoredLine
                        ? new Pen(Color.Gold, 3)
                        : Pens.Black;

                    g.DrawEllipse(borderPen,
                        xPos - pointRadius,
                        yPos - pointRadius,
                        pointRadius * 2,
                        pointRadius * 2);

                    if (point.IsPartOfScoredLine && borderPen != Pens.Black)
                    {
                        borderPen.Dispose();
                    }
                }
            }
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        _controller.HandleClick(e.X, e.Y, _margin, _cellSize);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        this.Invalidate();
    }
}
