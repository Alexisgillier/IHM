using System;
using System.Windows.Forms;
using System.Drawing;

namespace RucheMQTTApp
{
    public partial class FormHistory : Form
    {
        public FormHistory()
        {
            this.Text = "Historique Complet BDD";
            this.Size = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterParent;

            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false
            };

            this.Controls.Add(grid);
            this.Load += (s, e) => { grid.DataSource = DatabaseHelper.LoadAll(); };
        }
    }
}