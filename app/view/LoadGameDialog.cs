namespace app.view;

using app.model;
using app.controller;
using System.Drawing;
using System.Windows.Forms;
using DrawingPoint = System.Drawing.Point;

public class LoadGameDialog : Form
{
    private readonly List<GameSave> _saves;
    private readonly GameController _controller;
    private ListBox lstGames;
    private ListBox lstTurns;
    private Label lblGameInfo;
    private Button btnLoadTurn;
    private Button btnDelete;

    public LoadGameDialog(List<GameSave> saves, GameController controller)
    {
        _saves = saves;
        _controller = controller;
        InitializeComponent();
        PopulateGames();
    }

    private void InitializeComponent()
    {
        this.Text = "Charger une Partie";
        this.Size = new Size(600, 450);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(45, 45, 50);

        // Label pour les parties
        Label lblGames = new Label
        {
            Text = "Parties sauvegardees:",
            Location = new DrawingPoint(20, 15),
            Size = new Size(200, 20),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        // Liste des parties
        lstGames = new ListBox
        {
            Location = new DrawingPoint(20, 40),
            Size = new Size(250, 200),
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(60, 60, 70),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        lstGames.SelectedIndexChanged += LstGames_SelectedIndexChanged;

        // Informations sur la partie selectionnee
        lblGameInfo = new Label
        {
            Text = "Selectionnez une partie...",
            Location = new DrawingPoint(20, 250),
            Size = new Size(250, 60),
            ForeColor = Color.LightGray,
            Font = new Font("Segoe UI", 9)
        };

        // Label pour les tours
        Label lblTurns = new Label
        {
            Text = "Tours disponibles:",
            Location = new DrawingPoint(290, 15),
            Size = new Size(200, 20),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        // Liste des tours
        lstTurns = new ListBox
        {
            Location = new DrawingPoint(290, 40),
            Size = new Size(280, 270),
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(60, 60, 70),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        // Bouton charger tour
        btnLoadTurn = new Button
        {
            Text = "Charger ce tour",
            Location = new DrawingPoint(290, 320),
            Size = new Size(130, 35),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(80, 180, 80),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Enabled = false
        };
        btnLoadTurn.FlatAppearance.BorderSize = 0;
        btnLoadTurn.Click += BtnLoadTurn_Click;

        // Bouton supprimer
        btnDelete = new Button
        {
            Text = "Supprimer partie",
            Location = new DrawingPoint(20, 320),
            Size = new Size(130, 35),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(180, 80, 80),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Enabled = false
        };
        btnDelete.FlatAppearance.BorderSize = 0;
        btnDelete.Click += BtnDelete_Click;

        // Bouton fermer
        Button btnClose = new Button
        {
            Text = "Fermer",
            Location = new DrawingPoint(440, 320),
            Size = new Size(130, 35),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(100, 100, 110),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10)
        };
        btnClose.FlatAppearance.BorderSize = 0;

        this.Controls.Add(lblGames);
        this.Controls.Add(lstGames);
        this.Controls.Add(lblGameInfo);
        this.Controls.Add(lblTurns);
        this.Controls.Add(lstTurns);
        this.Controls.Add(btnLoadTurn);
        this.Controls.Add(btnDelete);
        this.Controls.Add(btnClose);

        this.CancelButton = btnClose;
    }

    private void PopulateGames()
    {
        lstGames.Items.Clear();
        foreach (var save in _saves)
        {
            string status = save.IsFinished ? " [Termine]" : "";
            string autoSave = save.AutoSave ? " [Auto]" : "";
            lstGames.Items.Add($"{save.Name}{status}{autoSave}");
        }
    }

    private async void LstGames_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (lstGames.SelectedIndex < 0 || lstGames.SelectedIndex >= _saves.Count)
            return;

        var save = _saves[lstGames.SelectedIndex];
        lblGameInfo.Text = $"Plateau: {save.BoardWidth}x{save.BoardHeight}\n" +
                          $"Cree le: {save.CreatedAt:dd/MM/yyyy HH:mm}\n" +
                          $"Modifie: {save.UpdatedAt:dd/MM/yyyy HH:mm}";

        btnDelete.Enabled = true;

        // Charger les tours
        lstTurns.Items.Clear();
        var turns = await _controller.GetTurnStatesAsync(save.Id);
        foreach (var turn in turns)
        {
            lstTurns.Items.Add(new TurnItem(turn));
        }
        btnLoadTurn.Enabled = turns.Count > 0;
    }

    private async void BtnLoadTurn_Click(object? sender, EventArgs e)
    {
        if (lstTurns.SelectedItem is TurnItem turnItem)
        {
            await _controller.LoadTurnStateAsync(turnItem.Turn.Id);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        else if (lstTurns.Items.Count > 0)
        {
            // Charger le dernier tour si aucun n'est selectionne
            if (lstTurns.Items[lstTurns.Items.Count - 1] is TurnItem lastTurn)
            {
                await _controller.LoadTurnStateAsync(lastTurn.Turn.Id);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }

    private async void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (lstGames.SelectedIndex < 0)
            return;

        var save = _saves[lstGames.SelectedIndex];
        var result = MessageBox.Show(
            $"Voulez-vous vraiment supprimer '{save.Name}'?\n\nCette action est irreversible.",
            "Confirmation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            await _controller.DeleteSaveAsync(save.Id);
            _saves.RemoveAt(lstGames.SelectedIndex);
            PopulateGames();
            lstTurns.Items.Clear();
            lblGameInfo.Text = "Selectionnez une partie...";
            btnDelete.Enabled = false;
            btnLoadTurn.Enabled = false;
        }
    }

    // Classe helper pour afficher les tours
    private class TurnItem
    {
        public TurnState Turn { get; }

        public TurnItem(TurnState turn)
        {
            Turn = turn;
        }

        public override string ToString()
        {
            return $"Tour {Turn.TurnNumber} - {Turn.ActionType} ({Turn.SavedAt:HH:mm:ss})";
        }
    }
}
