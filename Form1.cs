namespace Projet_Jeu_De_Ligne;

using app.controller;
using app.model;
using app.view;

public partial class Form1 : Form
{
    private GameController _controller;
    private PlateauView _plateauView;
    private NumericUpDown numX;
    private NumericUpDown numY;
    private Label labelJoueur;
    private Label labelScore;
    private Label labelCannonHint;
    private Label labelCannonStatus;
    private System.Windows.Forms.Timer? _feedbackTimer;

    public Form1()
    {
        _controller = new GameController();
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Jeu de Ligne - 5 points alignes";
        this.Size = new Size(850, 750);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(700, 600);

        // Activer la capture des touches clavier
        this.KeyPreview = true;
        this.KeyDown += Form1_KeyDown;

        // Panel de configuration avec design moderne
        Panel configPanel = new Panel
        {
            Location = new System.Drawing.Point(10, 10),
            Size = new Size(820, 90),
            BackColor = Color.FromArgb(60, 60, 70),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        Label labelX = new Label
        {
            Text = "Colonnes:",
            Location = new System.Drawing.Point(10, 15),
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9)
        };

        numX = new NumericUpDown
        {
            Location = new System.Drawing.Point(80, 12),
            Size = new Size(55, 25),
            Minimum = 5,
            Maximum = 40,
            Value = 10,
            Font = new Font("Segoe UI", 9)
        };

        Label labelY = new Label
        {
            Text = "Lignes:",
            Location = new System.Drawing.Point(145, 15),
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9)
        };

        numY = new NumericUpDown
        {
            Location = new System.Drawing.Point(200, 12),
            Size = new Size(55, 25),
            Minimum = 5,
            Maximum = 40,
            Value = 10,
            Font = new Font("Segoe UI", 9)
        };

        Button btnNouveau = new Button
        {
            Text = "Nouvelle Partie",
            Location = new System.Drawing.Point(270, 10),
            Size = new Size(110, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(80, 180, 80),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnNouveau.FlatAppearance.BorderSize = 0;
        btnNouveau.Click += BtnNouveau_Click;

        labelJoueur = new Label
        {
            Text = "Tour: Joueur 1",
            Location = new System.Drawing.Point(400, 12),
            AutoSize = true,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.DodgerBlue
        };

        labelScore = new Label
        {
            Text = "Joueur 1: 0 | Joueur 2: 0",
            Location = new System.Drawing.Point(10, 50),
            Size = new Size(350, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.LimeGreen
        };

        // Instructions du canon
        labelCannonHint = new Label
        {
            Text = "Canon: Glissez puis Ctrl+[1-9] pour tirer",
            Location = new System.Drawing.Point(10, 70),
            AutoSize = true,
            Font = new Font("Segoe UI", 8, FontStyle.Italic),
            ForeColor = Color.FromArgb(180, 180, 190)
        };

        // Status du canon (feedback)
        labelCannonStatus = new Label
        {
            Text = "",
            Location = new System.Drawing.Point(400, 50),
            Size = new Size(400, 35),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.Orange
        };

        configPanel.Controls.Add(labelX);
        configPanel.Controls.Add(numX);
        configPanel.Controls.Add(labelY);
        configPanel.Controls.Add(numY);
        configPanel.Controls.Add(btnNouveau);
        configPanel.Controls.Add(labelJoueur);
        configPanel.Controls.Add(labelScore);
        configPanel.Controls.Add(labelCannonHint);
        configPanel.Controls.Add(labelCannonStatus);

        // Panel du plateau utilisant PlateauView
        _plateauView = new PlateauView(_controller)
        {
            Location = new System.Drawing.Point(10, 110),
            Size = new Size(820, 590),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        // S'abonner aux evenements du controller
        _controller.OnPlayerChanged += OnPlayerChanged;
        _controller.OnScoreChanged += OnScoreChanged;
        _controller.OnCannonFired += OnCannonFired;

        this.Controls.Add(configPanel);
        this.Controls.Add(_plateauView);

        // Initialiser le jeu
        _controller.NewGame((int)numX.Value, (int)numY.Value);
        UpdateScoreLabel();

        // Timer pour les messages temporaires
        _feedbackTimer = new System.Windows.Forms.Timer { Interval = 2500 };
        _feedbackTimer.Tick += (s, e) =>
        {
            labelCannonStatus.Text = "";
            _feedbackTimer.Stop();
        };
    }

    private void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        // Verifier Ctrl + 1-9
        if (e.Control && e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
        {
            int speed = e.KeyCode - Keys.D0; // D1 = 1, D9 = 9
            var result = _controller.FireCannon(speed);

            if (result == null && !_controller.HasDraggedCannonThisTurn)
            {
                ShowCannonFeedback("Deplacez d'abord le canon pour viser!", Color.Orange);
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void OnCannonFired(CannonFireResult result)
    {
        if (result.HitInvulnerable)
        {
            ShowCannonFeedback("TIR BLOQUE! Le point est protege par une ligne.", Color.Gold);
        }
        else if (result.WasDestroyed && result.HitPoint != null)
        {
            ShowCannonFeedback($"TOUCHE! Point detruit en ({result.HitPoint.X}, {result.HitPoint.Y})", Color.LimeGreen);
        }
        else
        {
            ShowCannonFeedback("Tir manque - aucun point adverse touche.", Color.Gray);
        }
    }

    private void ShowCannonFeedback(string message, Color color)
    {
        labelCannonStatus.Text = message;
        labelCannonStatus.ForeColor = color;
        _feedbackTimer?.Stop();
        _feedbackTimer?.Start();
    }

    private void BtnNouveau_Click(object? sender, EventArgs e)
    {
        _controller.NewGame((int)numX.Value, (int)numY.Value);
        UpdateScoreLabel();
        labelCannonStatus.Text = "";
    }

    private void OnPlayerChanged(Player player)
    {
        labelJoueur.Text = $"Tour: {player.Name}";
        labelJoueur.ForeColor = player.Color;

        // Indicateur si le joueur peut encore placer ou doit tirer
        if (_controller.HasDraggedCannonThisTurn)
        {
            labelCannonHint.Text = "Canon vise - Appuyez Ctrl+[1-9] pour tirer!";
            labelCannonHint.ForeColor = Color.Yellow;
        }
        else
        {
            labelCannonHint.Text = "Canon: Glissez puis Ctrl+[1-9] pour tirer";
            labelCannonHint.ForeColor = Color.FromArgb(180, 180, 190);
        }
    }

    private void OnScoreChanged(Player player, int newScore)
    {
        UpdateScoreLabel();
    }

    private void UpdateScoreLabel()
    {
        labelScore.Text = _controller.GetScoreText();
    }
}
