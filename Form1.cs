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

    public Form1()
    {
        _controller = new GameController();
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Jeu de Ligne - 5 points alignés";
        this.Size = new Size(750, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(600, 500);

        // Panel de configuration
        Panel configPanel = new Panel
        {
            Location = new System.Drawing.Point(10, 10),
            Size = new Size(720, 80),
            BackColor = Color.LightGray,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        Label labelX = new Label
        {
            Text = "Colonnes (X):",
            Location = new System.Drawing.Point(10, 15),
            AutoSize = true
        };

        numX = new NumericUpDown
        {
            Location = new System.Drawing.Point(100, 12),
            Size = new Size(60, 25),
            Minimum = 5,
            Maximum = 20,
            Value = 10
        };

        Label labelY = new Label
        {
            Text = "Lignes (Y):",
            Location = new System.Drawing.Point(180, 15),
            AutoSize = true
        };

        numY = new NumericUpDown
        {
            Location = new System.Drawing.Point(260, 12),
            Size = new Size(60, 25),
            Minimum = 5,
            Maximum = 20,
            Value = 10
        };

        Button btnNouveau = new Button
        {
            Text = "Nouvelle Partie",
            Location = new System.Drawing.Point(340, 10),
            Size = new Size(120, 30)
        };
        btnNouveau.Click += BtnNouveau_Click;

        labelJoueur = new Label
        {
            Text = "Tour: Joueur 1",
            Location = new System.Drawing.Point(480, 15),
            AutoSize = true,
            Font = new Font("Arial", 10, FontStyle.Bold),
            ForeColor = Color.Blue
        };

        labelScore = new Label
        {
            Text = "Joueur 1: 0 | Joueur 2: 0",
            Location = new System.Drawing.Point(10, 50),
            Size = new Size(700, 25),
            Font = new Font("Arial", 11, FontStyle.Bold),
            ForeColor = Color.DarkGreen
        };

        configPanel.Controls.Add(labelX);
        configPanel.Controls.Add(numX);
        configPanel.Controls.Add(labelY);
        configPanel.Controls.Add(numY);
        configPanel.Controls.Add(btnNouveau);
        configPanel.Controls.Add(labelJoueur);
        configPanel.Controls.Add(labelScore);

        // Panel du plateau utilisant PlateauView
        _plateauView = new PlateauView(_controller)
        {
            Location = new System.Drawing.Point(10, 100),
            Size = new Size(720, 550),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        // S'abonner aux événements du controller
        _controller.OnPlayerChanged += OnPlayerChanged;
        _controller.OnScoreChanged += OnScoreChanged;

        this.Controls.Add(configPanel);
        this.Controls.Add(_plateauView);

        // Initialiser le jeu
        _controller.NewGame((int)numX.Value, (int)numY.Value);
        UpdateScoreLabel();
    }

    private void BtnNouveau_Click(object? sender, EventArgs e)
    {
        _controller.NewGame((int)numX.Value, (int)numY.Value);
        UpdateScoreLabel();
    }

    private void OnPlayerChanged(Player player)
    {
        labelJoueur.Text = $"Tour: {player.Name}";
        labelJoueur.ForeColor = player.Color;
    }

    private void OnScoreChanged(Player player, int newScore)
    {
        UpdateScoreLabel();
        // MessageBox.Show(
        //     $"{player.Name} a marqué un point!\n5 points alignés!",
        //     "Point marqué!",
        //     MessageBoxButtons.OK,
        //     MessageBoxIcon.Information);
    }

    private void UpdateScoreLabel()
    {
        labelScore.Text = _controller.GetScoreText();
    }
}
