namespace Projet_Jeu_De_Ligne;

using System.Drawing.Drawing2D;
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
    private Label labelSaveStatus;
    private CheckBox chkAutoSave;
    private System.Windows.Forms.Timer? _feedbackTimer;

    public Form1()
    {
        _controller = new GameController();
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Jeu de Ligne - 5 points alignés";
        this.Size = new Size(1180, 870);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(1150, 800);
        this.BackColor = Color.FromArgb(240, 242, 245);

        this.KeyPreview = true;
        this.KeyDown += Form1_KeyDown;

        // Panel de configuration avec design moderne
        Panel configPanel = new Panel
        {
            Location = new System.Drawing.Point(15, 15),
            Size = new Size(1140, 105),
            BackColor = Color.FromArgb(255, 255, 255),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Ajouter ombre et bordure arrondie via Paint event
        configPanel.Paint += (s, e) =>
        {
            // Ombre
            Rectangle shadowRect = new Rectangle(3, 3, configPanel.Width - 3, configPanel.Height - 3);
            using (var shadowBrush = new SolidBrush(Color.FromArgb(25, 0, 0, 0)))
            {
                e.Graphics.FillRoundedRectangle(shadowBrush, shadowRect, 8);
            }

            // Fond blanc avec bordure
            Rectangle bgRect = new Rectangle(0, 0, configPanel.Width - 3, configPanel.Height - 3);
            using (var bgBrush = new LinearGradientBrush(bgRect,
                Color.FromArgb(255, 255, 255),
                Color.FromArgb(248, 250, 252),
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRoundedRectangle(bgBrush, bgRect, 8);
            }

            // Bordure subtile
            using (var borderPen = new Pen(Color.FromArgb(200, 210, 220), 1))
            {
                e.Graphics.DrawRoundedRectangle(borderPen, bgRect, 8);
            }
        };

        int marginX = 20;
        int marginY = 18;
        int spacingX = 12;
        int currentX = marginX;

        // Colonnes
        Label labelX = new Label
        {
            Text = "Colonnes:",
            AutoSize = true,
            ForeColor = Color.FromArgb(60, 70, 80),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new System.Drawing.Point(currentX, marginY + 2)
        };
        currentX += labelX.Width + spacingX;

        numX = new NumericUpDown
        {
            Size = new Size(65, 28),
            Minimum = 5,
            Maximum = 40,
            Value = 10,
            Font = new Font("Segoe UI", 10),
            Location = new System.Drawing.Point(currentX, marginY)
        };
        currentX += numX.Width + spacingX + 10;

        // Lignes
        Label labelY = new Label
        {
            Text = "Lignes:",
            AutoSize = true,
            ForeColor = Color.FromArgb(60, 70, 80),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new System.Drawing.Point(currentX, marginY + 2)
        };
        currentX += labelY.Width + spacingX;

        numY = new NumericUpDown
        {
            Size = new Size(65, 28),
            Minimum = 5,
            Maximum = 40,
            Value = 10,
            Font = new Font("Segoe UI", 10),
            Location = new System.Drawing.Point(currentX, marginY)
        };
        currentX += numY.Width + spacingX + 18;

        // Bouton Nouvelle Partie
        Button btnNouveau = new Button
        {
            Text = "🎮 Nouvelle Partie",
            Size = new Size(165, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Location = new System.Drawing.Point(currentX, marginY - 2)
        };
        btnNouveau.FlatAppearance.BorderSize = 0;
        btnNouveau.FlatAppearance.BorderColor = Color.FromArgb(16, 185, 129);
        btnNouveau.Click += BtnNouveau_Click;
        currentX += btnNouveau.Width + 25;

        // Label Joueur
        labelJoueur = new Label
        {
            Text = "🎯 Tour: Joueur 1",
            AutoSize = true,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.DodgerBlue,
            Location = new System.Drawing.Point(currentX, marginY + 4)
        };

        // Sauvegarde - première ligne, à droite
        int saveX = 870;
        Button btnSave = new Button
        {
            Text = "💾 Sauvegarder",
            Size = new Size(125, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(59, 130, 246),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Location = new System.Drawing.Point(saveX, marginY - 2)
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += BtnSave_Click;

        Button btnLoad = new Button
        {
            Text = "📂 Charger",
            Size = new Size(110, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(139, 92, 246),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Location = new System.Drawing.Point(saveX + 135, marginY - 2)
        };
        btnLoad.FlatAppearance.BorderSize = 0;
        btnLoad.Click += BtnLoad_Click;

        // Score - deuxième ligne
        labelScore = new Label
        {
            Text = "Joueur 1: 0  |  Joueur 2: 0",
            Size = new Size(280, 25),
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(16, 185, 129),
            Location = new System.Drawing.Point(marginX, marginY + 48),
            BackColor = Color.Transparent
        };

        // Canon Hint - deuxième ligne
        labelCannonHint = new Label
        {
            Text = "💣 Canon: Glissez puis Ctrl+[1-9] pour tirer",
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Italic),
            ForeColor = Color.FromArgb(120, 130, 140),
            Location = new System.Drawing.Point(marginX + 300, marginY + 51),
            BackColor = Color.Transparent,
            Visible = true
        };

        // Canon Status - deuxième ligne, même position que hint (caché par défaut)
        labelCannonStatus = new Label
        {
            Text = "",
            Size = new Size(380, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.Orange,
            Location = new System.Drawing.Point(marginX + 300, marginY + 48),
            BackColor = Color.White,
            Visible = false
        };

        chkAutoSave = new CheckBox
        {
            Text = "Auto-save",
            AutoSize = true,
            ForeColor = Color.FromArgb(60, 70, 80),
            Font = new Font("Segoe UI", 9),
            Checked = false,
            Location = new System.Drawing.Point(saveX, marginY + 42),
            BackColor = Color.Transparent
        };
        chkAutoSave.CheckedChanged += ChkAutoSave_Changed;

        labelSaveStatus = new Label
        {
            Text = "",
            Size = new Size(140, 22),
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(16, 185, 129),
            Location = new System.Drawing.Point(saveX + 135, marginY + 44),
            BackColor = Color.Transparent
        };

        // Ajouter tous les composants au panel (ordre important pour z-index)
        configPanel.Controls.AddRange(new Control[]
        {
        labelX, numX, labelY, numY, btnNouveau, labelJoueur,
        btnSave, btnLoad, chkAutoSave, labelSaveStatus,
        labelScore, labelCannonHint, labelCannonStatus // labelCannonStatus en dernier pour être au-dessus
        });

        // Plateau
        _plateauView = new PlateauView(_controller)
        {
            Location = new System.Drawing.Point(15, 130),
            Size = new Size(1140, 690),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        // Abonnements events
        _controller.OnPlayerChanged += OnPlayerChanged;
        _controller.OnScoreChanged += OnScoreChanged;
        _controller.OnCannonFired += OnCannonFired;
        _controller.OnSaveStatusChanged += OnSaveStatusChanged;
        _controller.OnSaveError += OnSaveError;

        this.Controls.Add(configPanel);
        this.Controls.Add(_plateauView);

        _controller.NewGame((int)numX.Value, (int)numY.Value);
        UpdateScoreLabel();

        _ = _controller.InitializeDatabaseAsync();

        _feedbackTimer = new System.Windows.Forms.Timer { Interval = 2500 };
        _feedbackTimer.Tick += (s, e) =>
        {
            labelCannonStatus.Text = "";
            labelCannonStatus.Visible = false;
            labelCannonHint.Visible = true;
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
        else if (result.WasReplaced && result.RestoredForPlayer != null)
        {
            ShowCannonFeedback($"REMPLACE! Point en ({result.TargetX}, {result.Y}) -> {result.RestoredForPlayer.Name}", Color.Orange);
        }
        else if (result.WasRestored && result.RestoredForPlayer != null)
        {
            ShowCannonFeedback($"RESTAURE! Point cree en ({result.TargetX}, {result.Y}) pour {result.RestoredForPlayer.Name}", Color.Cyan);
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
        labelCannonStatus.Visible = true;
        labelCannonHint.Visible = false;
        _feedbackTimer?.Stop();
        _feedbackTimer?.Start();
    }

    private void BtnNouveau_Click(object? sender, EventArgs e)
    {
        _controller.NewGame((int)numX.Value, (int)numY.Value);
        UpdateScoreLabel();
        labelCannonStatus.Text = "";
        labelCannonStatus.Visible = false;
        labelCannonHint.Visible = true;
    }

    private void OnPlayerChanged(Player player)
    {
        labelJoueur.Text = $"🎯 Tour: {player.Name}";
        labelJoueur.ForeColor = player.Color;

        // Indicateur si le joueur peut encore placer ou doit tirer
        if (_controller.HasDraggedCannonThisTurn)
        {
            labelCannonHint.Text = "💥 Canon visé - Appuyez Ctrl+[1-9] pour tirer!";
            labelCannonHint.ForeColor = Color.FromArgb(255, 180, 0);
        }
        else
        {
            labelCannonHint.Text = "💣 Canon: Glissez puis Ctrl+[1-9] pour tirer";
            labelCannonHint.ForeColor = Color.FromArgb(120, 130, 140);
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

    private void OnSaveStatusChanged(string message)
    {
        labelSaveStatus.Text = message;
        labelSaveStatus.ForeColor = Color.FromArgb(16, 185, 129);
    }

    private void OnSaveError(string message)
    {
        labelSaveStatus.Text = message;
        labelSaveStatus.ForeColor = Color.FromArgb(239, 68, 68);
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (_controller.CurrentGameSaveId == 0)
        {
            // Premiere sauvegarde - demander un nom
            using var dialog = new SaveGameDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var id = await _controller.CreateSaveAsync(dialog.SaveName, chkAutoSave.Checked);
                if (id > 0)
                {
                    labelSaveStatus.Text = $"Partie #{id} creee";
                }
            }
        }
        else
        {
            // Sauvegarde manuelle
            await _controller.ManualSaveAsync();
        }
    }

    private async void BtnLoad_Click(object? sender, EventArgs e)
    {
        var saves = await _controller.GetSavedGamesAsync();
        if (saves.Count == 0)
        {
            MessageBox.Show("Aucune partie sauvegardee.", "Charger", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new LoadGameDialog(saves, _controller);
        dialog.ShowDialog();
    }

    private void ChkAutoSave_Changed(object? sender, EventArgs e)
    {
        _controller.SetAutoSave(chkAutoSave.Checked);
    }
}
