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
        this.Text = "Nouvelle Sauvegarde";
        this.Size = new Size(350, 150);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(45, 45, 50);

        Label label = new Label
        {
            Text = "Nom de la partie:",
            Location = new Point(20, 20),
            Size = new Size(120, 20),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10)
        };

        txtName = new TextBox
        {
            Location = new Point(20, 45),
            Size = new Size(290, 25),
            Font = new Font("Segoe UI", 10),
            Text = $"Partie_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        Button btnOk = new Button
        {
            Text = "Creer",
            Location = new Point(130, 80),
            Size = new Size(80, 30),
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(80, 180, 80),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, e) => SaveName = txtName.Text;

        Button btnCancel = new Button
        {
            Text = "Annuler",
            Location = new Point(220, 80),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(100, 100, 110),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9)
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
