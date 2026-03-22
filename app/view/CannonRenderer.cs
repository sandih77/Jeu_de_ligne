namespace app.view;

using app.model;
using System.Drawing;
using System.Drawing.Drawing2D;

public static class CannonRenderer
{
    public const int CannonWidth = 45;
    public const int CannonHeight = 28;
    public const int BarrelLength = 35;
    public const int BarrelWidth = 10;

    /// <summary>
    /// Dessine un canon a la position specifiee.
    /// </summary>
    public static void DrawCannon(Graphics g, Cannon cannon, int xPos, int yPos, bool isActive, bool isDragging)
    {
        // Corps du canon
        Rectangle bodyRect;
        Rectangle barrelRect;

        if (cannon.Side == CannonSide.Left)
        {
            bodyRect = new Rectangle(xPos - CannonWidth + 5, yPos - CannonHeight / 2,
                                     CannonWidth, CannonHeight);
            barrelRect = new Rectangle(xPos + 5, yPos - BarrelWidth / 2,
                                       BarrelLength, BarrelWidth);
        }
        else
        {
            bodyRect = new Rectangle(xPos - 5, yPos - CannonHeight / 2,
                                     CannonWidth, CannonHeight);
            barrelRect = new Rectangle(xPos - 5 - BarrelLength, yPos - BarrelWidth / 2,
                                       BarrelLength, BarrelWidth);
        }

        // Ombre du canon
        Rectangle shadowRect = new Rectangle(bodyRect.X + 3, bodyRect.Y + 3, bodyRect.Width, bodyRect.Height);
        using (var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
        {
            g.FillRoundedRectangle(shadowBrush, shadowRect, 6);
        }

        // Corps avec degrade metallique
        using (var bodyBrush = new LinearGradientBrush(bodyRect,
            Color.FromArgb(80, 80, 90),
            Color.FromArgb(50, 50, 60),
            LinearGradientMode.Vertical))
        {
            g.FillRoundedRectangle(bodyBrush, bodyRect, 6);
        }

        // Reflet sur le corps
        Rectangle highlightRect = new Rectangle(bodyRect.X + 2, bodyRect.Y + 2, bodyRect.Width - 4, bodyRect.Height / 3);
        using (var highlightBrush = new LinearGradientBrush(highlightRect,
            Color.FromArgb(60, 255, 255, 255),
            Color.FromArgb(0, 255, 255, 255),
            LinearGradientMode.Vertical))
        {
            g.FillRoundedRectangle(highlightBrush, highlightRect, 4);
        }

        // Canon (barrel) avec degrade
        using (var barrelBrush = new LinearGradientBrush(barrelRect,
            Color.FromArgb(100, 100, 110),
            Color.FromArgb(60, 60, 70),
            LinearGradientMode.Vertical))
        {
            g.FillRoundedRectangle(barrelBrush, barrelRect, 3);
        }

        // Bordure du canon
        using (var barrelPen = new Pen(Color.FromArgb(40, 40, 50), 1))
        {
            g.DrawRoundedRectangle(barrelPen, barrelRect, 3);
        }

        // Embouchure du canon
        Rectangle muzzleRect;
        if (cannon.Side == CannonSide.Left)
        {
            muzzleRect = new Rectangle(barrelRect.Right - 6, barrelRect.Y - 2, 6, barrelRect.Height + 4);
        }
        else
        {
            muzzleRect = new Rectangle(barrelRect.X, barrelRect.Y - 2, 6, barrelRect.Height + 4);
        }
        using (var muzzleBrush = new SolidBrush(Color.FromArgb(70, 70, 80)))
        {
            g.FillRoundedRectangle(muzzleBrush, muzzleRect, 2);
        }

        // Indicateur de couleur du joueur (cercle colore)
        int indicatorSize = 14;
        Rectangle indicatorRect = new Rectangle(
            bodyRect.X + (bodyRect.Width - indicatorSize) / 2,
            bodyRect.Y + (bodyRect.Height - indicatorSize) / 2,
            indicatorSize, indicatorSize);

        using (var indicatorBrush = new SolidBrush(cannon.Owner.Color))
        {
            g.FillEllipse(indicatorBrush, indicatorRect);
        }
        using (var indicatorPen = new Pen(Color.White, 2))
        {
            g.DrawEllipse(indicatorPen, indicatorRect);
        }

        // Bordure du corps (active/inactive)
        Color borderColor = isActive ? (isDragging ? Color.Yellow : Color.LimeGreen) : Color.FromArgb(40, 40, 50);
        int borderWidth = isActive ? 3 : 1;
        using (var borderPen = new Pen(borderColor, borderWidth))
        {
            g.DrawRoundedRectangle(borderPen, bodyRect, 6);
        }

        // Effet de brillance si actif
        if (isActive && !isDragging)
        {
            using (var glowPen = new Pen(Color.FromArgb(50, Color.LimeGreen), 5))
            {
                g.DrawRoundedRectangle(glowPen, bodyRect, 6);
            }
        }
    }

    /// <summary>
    /// Dessine le projectile pendant l'animation de tir.
    /// </summary>
    public static void DrawProjectile(Graphics g, int x, int y, Color playerColor)
    {
        int size = 12;

        // Trainee
        for (int i = 3; i > 0; i--)
        {
            int trailX = x - i * 8;
            int alpha = 80 - i * 20;
            using (var trailBrush = new SolidBrush(Color.FromArgb(alpha, playerColor)))
            {
                g.FillEllipse(trailBrush, trailX - size / 3, y - size / 3, size * 2 / 3, size * 2 / 3);
            }
        }

        // Projectile principal avec gradient radial
        Rectangle projectileRect = new Rectangle(x - size / 2, y - size / 2, size, size);
        using (var path = new GraphicsPath())
        {
            path.AddEllipse(projectileRect);
            using (var brush = new PathGradientBrush(path))
            {
                brush.CenterColor = Color.White;
                brush.SurroundColors = new[] { playerColor };
                g.FillEllipse(brush, projectileRect);
            }
        }

        // Bordure
        using (var pen = new Pen(Color.FromArgb(200, playerColor), 2))
        {
            g.DrawEllipse(pen, projectileRect);
        }
    }

    /// <summary>
    /// Dessine la ligne de visee.
    /// </summary>
    public static void DrawAimLine(Graphics g, int startX, int y, int endX, Color playerColor)
    {
        using (var pen = new Pen(Color.FromArgb(100, playerColor), 2))
        {
            pen.DashStyle = DashStyle.Dash;
            pen.DashPattern = new float[] { 8, 4 };
            g.DrawLine(pen, startX, y, endX, y);
        }

        // Point de visee
        int targetSize = 8;
        using (var brush = new SolidBrush(Color.FromArgb(150, playerColor)))
        {
            g.FillEllipse(brush, endX - targetSize / 2, y - targetSize / 2, targetSize, targetSize);
        }
        using (var pen = new Pen(playerColor, 2))
        {
            g.DrawEllipse(pen, endX - targetSize / 2, y - targetSize / 2, targetSize, targetSize);
        }
    }

    /// <summary>
    /// Dessine l'effet d'explosion quand un point est detruit.
    /// </summary>
    public static void DrawExplosion(Graphics g, int x, int y, int frame, Color color)
    {
        int maxSize = 30 + frame * 5;
        int alpha = Math.Max(0, 200 - frame * 40);

        for (int i = 0; i < 8; i++)
        {
            double angle = i * Math.PI / 4 + frame * 0.2;
            int particleX = x + (int)(Math.Cos(angle) * (frame * 3));
            int particleY = y + (int)(Math.Sin(angle) * (frame * 3));
            int particleSize = Math.Max(2, 8 - frame);

            using (var brush = new SolidBrush(Color.FromArgb(alpha, color)))
            {
                g.FillEllipse(brush, particleX - particleSize / 2, particleY - particleSize / 2,
                              particleSize, particleSize);
            }
        }

        // Cercle central
        using (var brush = new SolidBrush(Color.FromArgb(alpha / 2, Color.White)))
        {
            int size = maxSize / 2;
            g.FillEllipse(brush, x - size / 2, y - size / 2, size, size);
        }
    }

    /// <summary>
    /// Dessine l'effet de blocage quand le tir touche un point invulnerable.
    /// </summary>
    public static void DrawShieldEffect(Graphics g, int x, int y, int frame)
    {
        int alpha = Math.Max(0, 200 - frame * 30);
        int size = 20 + frame * 2;

        using (var pen = new Pen(Color.FromArgb(alpha, Color.Gold), 3))
        {
            g.DrawEllipse(pen, x - size / 2, y - size / 2, size, size);
        }

        // Petites etoiles
        using (var brush = new SolidBrush(Color.FromArgb(alpha, Color.Yellow)))
        {
            for (int i = 0; i < 4; i++)
            {
                double angle = i * Math.PI / 2 + frame * 0.3;
                int starX = x + (int)(Math.Cos(angle) * (size / 2 + 5));
                int starY = y + (int)(Math.Sin(angle) * (size / 2 + 5));
                g.FillEllipse(brush, starX - 3, starY - 3, 6, 6);
            }
        }
    }
}

/// <summary>
/// Extensions pour dessiner des rectangles arrondis.
/// </summary>
public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
    {
        using (var path = GetRoundedRectPath(rect, radius))
        {
            g.FillPath(brush, path);
        }
    }

    public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle rect, int radius)
    {
        using (var path = GetRoundedRectPath(rect, radius))
        {
            g.DrawPath(pen, path);
        }
    }

    private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int diameter = radius * 2;

        if (diameter > rect.Width) diameter = rect.Width;
        if (diameter > rect.Height) diameter = rect.Height;

        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}
