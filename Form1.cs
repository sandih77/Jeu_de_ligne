namespace Projet_Jeu_De_Ligne;

public partial class Form1 : Form
{
    private int gridX = 10; // Nombre de colonnes
    private int gridY = 10; // Nombre de lignes
    private int cellSize = 40; // Taille d'une cellule
    private int margin = 50; // Marge autour du plateau

    private int[,] plateau; // 0 = vide, 1 = joueur 1, 2 = joueur 2
    private int joueurActuel = 1;

    private Panel plateauPanel;
    private NumericUpDown numX;
    private NumericUpDown numY;
    private Label labelJoueur;

    public Form1()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Jeu de Ligne";
        this.Size = new Size(700, 650);
        this.StartPosition = FormStartPosition.CenterScreen;

        // Panel de configuration
        Panel configPanel = new Panel
        {
            Location = new Point(10, 10),
            Size = new Size(660, 50),
            BackColor = Color.LightGray
        };

        Label labelX = new Label
        {
            Text = "Colonnes (X):",
            Location = new Point(10, 15),
            AutoSize = true
        };

        numX = new NumericUpDown
        {
            Location = new Point(100, 12),
            Size = new Size(60, 25),
            Minimum = 3,
            Maximum = 20,
            Value = gridX
        };

        Label labelY = new Label
        {
            Text = "Lignes (Y):",
            Location = new Point(180, 15),
            AutoSize = true
        };

        numY = new NumericUpDown
        {
            Location = new Point(260, 12),
            Size = new Size(60, 25),
            Minimum = 3,
            Maximum = 20,
            Value = gridY
        };

        Button btnNouveau = new Button
        {
            Text = "Nouvelle Partie",
            Location = new Point(340, 10),
            Size = new Size(120, 30)
        };
        btnNouveau.Click += BtnNouveau_Click;

        labelJoueur = new Label
        {
            Text = "Tour: Joueur 1",
            Location = new Point(480, 15),
            AutoSize = true,
            Font = new Font("Arial", 10, FontStyle.Bold),
            ForeColor = Color.Blue
        };

        configPanel.Controls.Add(labelX);
        configPanel.Controls.Add(numX);
        configPanel.Controls.Add(labelY);
        configPanel.Controls.Add(numY);
        configPanel.Controls.Add(btnNouveau);
        configPanel.Controls.Add(labelJoueur);

        // Panel du plateau
        plateauPanel = new Panel
        {
            Location = new Point(10, 70),
            Size = new Size(660, 530),
            BackColor = Color.Beige,
            BorderStyle = BorderStyle.FixedSingle
        };
        plateauPanel.Paint += PlateauPanel_Paint;
        plateauPanel.MouseClick += PlateauPanel_MouseClick;

        this.Controls.Add(configPanel);
        this.Controls.Add(plateauPanel);

        // Initialiser le plateau
        InitialiserPlateau();
    }

    private void InitialiserPlateau()
    {
        gridX = (int)numX.Value;
        gridY = (int)numY.Value;
        plateau = new int[gridX, gridY];
        joueurActuel = 1;
        MettreAJourLabelJoueur();
        plateauPanel.Invalidate();
    }

    private void BtnNouveau_Click(object? sender, EventArgs e)
    {
        InitialiserPlateau();
    }

    private void PlateauPanel_Paint(object? sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Calculer la taille des cellules pour s'adapter au panel
        int availableWidth = plateauPanel.Width - 2 * margin;
        int availableHeight = plateauPanel.Height - 2 * margin;
        cellSize = Math.Min(availableWidth / (gridX - 1), availableHeight / (gridY - 1));

        // Dessiner les lignes verticales
        Pen linePen = new Pen(Color.Black, 2);
        for (int x = 0; x < gridX; x++)
        {
            int xPos = margin + x * cellSize;
            g.DrawLine(linePen, xPos, margin, xPos, margin + (gridY - 1) * cellSize);
        }

        // Dessiner les lignes horizontales
        for (int y = 0; y < gridY; y++)
        {
            int yPos = margin + y * cellSize;
            g.DrawLine(linePen, margin, yPos, margin + (gridX - 1) * cellSize, yPos);
        }

        // Dessiner les points placés
        int pointRadius = cellSize / 4;
        for (int x = 0; x < gridX; x++)
        {
            for (int y = 0; y < gridY; y++)
            {
                if (plateau[x, y] != 0)
                {
                    int xPos = margin + x * cellSize;
                    int yPos = margin + y * cellSize;

                    Brush brush = plateau[x, y] == 1 ? Brushes.Blue : Brushes.Red;
                    g.FillEllipse(brush,
                        xPos - pointRadius,
                        yPos - pointRadius,
                        pointRadius * 2,
                        pointRadius * 2);

                    // Bordure du point
                    g.DrawEllipse(Pens.Black,
                        xPos - pointRadius,
                        yPos - pointRadius,
                        pointRadius * 2,
                        pointRadius * 2);
                }
            }
        }

        linePen.Dispose();
    }

    private void PlateauPanel_MouseClick(object? sender, MouseEventArgs e)
    {
        // Trouver l'intersection la plus proche
        int clickX = e.X - margin;
        int clickY = e.Y - margin;

        // Arrondir à l'intersection la plus proche
        int gridPosX = (int)Math.Round((double)clickX / cellSize);
        int gridPosY = (int)Math.Round((double)clickY / cellSize);

        // Vérifier si le clic est proche d'une intersection (tolérance)
        int tolerance = cellSize / 3;
        int exactX = gridPosX * cellSize;
        int exactY = gridPosY * cellSize;

        if (Math.Abs(clickX - exactX) <= tolerance && Math.Abs(clickY - exactY) <= tolerance)
        {
            // Vérifier les limites
            if (gridPosX >= 0 && gridPosX < gridX && gridPosY >= 0 && gridPosY < gridY)
            {
                // Vérifier si l'intersection est libre
                if (plateau[gridPosX, gridPosY] == 0)
                {
                    plateau[gridPosX, gridPosY] = joueurActuel;
                    joueurActuel = joueurActuel == 1 ? 2 : 1;
                    MettreAJourLabelJoueur();
                    plateauPanel.Invalidate();
                }
            }
        }
    }

    private void MettreAJourLabelJoueur()
    {
        labelJoueur.Text = $"Tour: Joueur {joueurActuel}";
        labelJoueur.ForeColor = joueurActuel == 1 ? Color.Blue : Color.Red;
    }
}
