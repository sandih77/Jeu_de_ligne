namespace app.view;

using System.Drawing;
using System.Windows.Forms;

public class SaveGameDialog : Form
{
    public string SaveName { get; private set; } = "";
    private TextBox txtName;

    public SaveGameDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "💾 Nouvelle Sauvegarde";
        this.Size = new Size(450, 200);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(248, 250, 252);

        Label label = new Label
        {
            Text = "📝 Nom de la partie:",
            Location = new Point(30, 30),
            Size = new Size(180, 25),
            ForeColor = Color.FromArgb(60, 70, 80),
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };

        txtName = new TextBox
        {
            Location = new Point(30, 65),
            Size = new Size(380, 35),
            Font = new Font("Segoe UI", 11),
            Text = $"Partie_{DateTime.Now:yyyyMMdd_HHmmss}",
            BorderStyle = BorderStyle.FixedSingle
        };

        Button btnOk = new Button
        {
            Text = "✅ Créer",
            Location = new Point(150, 120),
            Size = new Size(120, 40),
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, e) => SaveName = txtName.Text;

        Button btnCancel = new Button
        {
            Text = "❌ Annuler",
            Location = new Point(280, 120),
            Size = new Size(120, 40),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(156, 163, 175),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;

        this.Controls.Add(label);
        this.Controls.Add(txtName);
        this.Controls.Add(btnOk);
        this.Controls.Add(btnCancel);

        this.AcceptButton = btnOk;
        this.CancelButton = btnCancel;
    }
}
