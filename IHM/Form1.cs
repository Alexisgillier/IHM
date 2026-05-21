using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMarker = GMap.NET.WindowsForms.Markers.GMarkerGoogle;
using GMap.NET.MapProviders;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using WinCharts = LiveCharts.WinForms;

namespace RucheMQTTApp
{
    public partial class Form1 : Form
    {
        private RecevoirMqtt _mqttService;
        private int _selectedId = 1;
        private WinCharts.CartesianChart chartT, chartP, chartH;
        private Label lblT, lblP, lblH, lblAlerte;
        private Panel topPanel, sidePanel, pnlAlerte;
        private GMapControl map;
        private GMapOverlay markersOverlay;
        private ComboBox comboRuches;
        private DataGridView gridLogs;
        private Timer blinkTimer;
        private bool isBlink = false;
        private int _idAlerteEnCours = -1;

        // Stockage des données
        private Dictionary<int, ChartValues<DateTimePoint>> histT = new Dictionary<int, ChartValues<DateTimePoint>>();
        private Dictionary<int, ChartValues<DateTimePoint>> histP = new Dictionary<int, ChartValues<DateTimePoint>>();
        private Dictionary<int, ChartValues<DateTimePoint>> histH = new Dictionary<int, ChartValues<DateTimePoint>>();
        private Dictionary<int, PointLatLng> positions = new Dictionary<int, PointLatLng>();
        private Dictionary<int, string> messagesAlerte = new Dictionary<int, string>();
        private Dictionary<int, bool> etatsAcquits = new Dictionary<int, bool>();

        public Form1()
        {
            GMapProvider.UserAgent = "BeeMonitorPro_" + Guid.NewGuid().ToString().Substring(0, 5);
            InitInterface();

            _mqttService = new RecevoirMqtt();

            // Événements MQTT
            _mqttService.OnDataReceived += (data) => {
                this.Invoke(new Action(() => ProcessData(data)));
            };

            _mqttService.OnLog += (tag, msg, color) => {
                this.Invoke(new Action(() => Log(tag, msg, color)));
            };

            this.Load += async (s, e) => {
                await Task.Delay(500);
                SafeInitMap();
                Log("DEBUG", "--- DÉBUT INITIALISATION ---", Color.Purple);
                ChargerHistoriqueDepuisBDD();
                await _mqttService.Connecter();
            };

            // Timer pour les alertes (Vol / Essaimage)
            blinkTimer = new Timer { Interval = 500 };
            blinkTimer.Tick += (s, e) => {
                isBlink = !isBlink;
                if (pnlAlerte != null) pnlAlerte.BackColor = isBlink ? Color.Red : Color.Black;
            };
        }

        private void ChargerHistoriqueDepuisBDD()
        {
            try
            {
                Log("BDD", "Lecture de la base de données...", Color.Blue);
                List<Mesure> historique = DatabaseHelper.LoadAll();

                if (historique == null)
                {
                    Log("ERR", "Serveur injoignable ou erreur SQL.", Color.Red);
                    return;
                }

                Log("BDD", $"{historique.Count} lignes récupérées.", Color.DarkBlue);

                var historiqueTrie = historique.OrderBy(m => m.DateMesure).ToList();
                int pointsValides = 0;

                foreach (var m in historiqueTrie)
                {
                    int id = m.RucheId;
                    if (id <= 0) continue;

                    if (!histT.ContainsKey(id))
                    {
                        histT[id] = new ChartValues<DateTimePoint>();
                        histP[id] = new ChartValues<DateTimePoint>();
                        histH[id] = new ChartValues<DateTimePoint>();
                        positions[id] = new PointLatLng(m.Latitude ?? 48.85, m.Longitude ?? 2.35);
                        etatsAcquits[id] = true;

                        if (!comboRuches.Items.Contains("Ruche n°" + id))
                            comboRuches.Items.Add("Ruche n°" + id);
                    }

                    if (m.Temp.HasValue) histT[id].Add(new DateTimePoint(m.DateMesure, m.Temp.Value));
                    if (m.Poids.HasValue) histP[id].Add(new DateTimePoint(m.DateMesure, m.Poids.Value));
                    if (m.Humid.HasValue) histH[id].Add(new DateTimePoint(m.DateMesure, m.Humid.Value));
                    pointsValides++;
                }

                UpdateUI();
                Log("OK", $"Historique chargé : {pointsValides} points.", Color.Green);
            }
            catch (Exception ex)
            {
                Log("CRIT", "Erreur BDD : " + ex.Message, Color.Red);
            }
        }

        private void ProcessData(BeeData d)
        {
            if (d.RucheId <= 0) return;

            // Sauvegarde automatique
            DatabaseHelper.SaveBeeData(d);
            Log("MQTT", $"Données reçues pour Ruche {d.RucheId}", Color.DarkGreen);

            if (!histT.ContainsKey(d.RucheId))
            {
                histT[d.RucheId] = new ChartValues<DateTimePoint>();
                histP[d.RucheId] = new ChartValues<DateTimePoint>();
                histH[d.RucheId] = new ChartValues<DateTimePoint>();
                positions[d.RucheId] = d.Location ?? new PointLatLng(48.85, 2.35);
                etatsAcquits[d.RucheId] = true;
                if (!comboRuches.Items.Contains("Ruche n°" + d.RucheId))
                    comboRuches.Items.Add("Ruche n°" + d.RucheId);
            }

            if (d.Temperature.HasValue) AddPt(histT[d.RucheId], d.Temperature.Value);
            if (d.PoidsKg.HasValue) AddPt(histP[d.RucheId], d.PoidsKg.Value);
            if (d.Humidite.HasValue) AddPt(histH[d.RucheId], d.Humidite.Value);
            if (d.Location.HasValue) positions[d.RucheId] = d.Location.Value;

            // Détection Alertes
            if (d.VolAlerte == "OUI")
            {
                messagesAlerte[d.RucheId] = $"⚠️ VOL EN COURS - Ruche n°{d.RucheId}";
                etatsAcquits[d.RucheId] = false;
            }
            if (d.AlerteEssaimage == "OUI")
            {
                messagesAlerte[d.RucheId] = $"🐝 ESSAIMAGE - Ruche n°{d.RucheId}";
                etatsAcquits[d.RucheId] = false;
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (histT == null || !histT.ContainsKey(_selectedId)) return;

            // Maj Graphiques et Labels
            if (chartT != null && chartT.Series.Count > 0)
            {
                chartT.Series[0].Values = histT[_selectedId];
                lblT.Text = histT[_selectedId].Count > 0 ? $"{histT[_selectedId].Last().Value:0.0}°C" : "--";
            }
            if (chartP != null && chartP.Series.Count > 0)
            {
                chartP.Series[0].Values = histP[_selectedId];
                lblP.Text = histP[_selectedId].Count > 0 ? $"{histP[_selectedId].Last().Value:0.0}kg" : "--";
            }
            if (chartH != null && chartH.Series.Count > 0)
            {
                chartH.Series[0].Values = histH[_selectedId];
                lblH.Text = histH[_selectedId].Count > 0 ? $"{histH[_selectedId].Last().Value:0}%" : "--";
            }

            // Maj Carte
            if (markersOverlay != null)
            {
                markersOverlay.Markers.Clear();
                foreach (var pos in positions)
                {
                    var m = new GMarker(pos.Value, GMap.NET.WindowsForms.Markers.GMarkerGoogleType.red_dot)
                    { ToolTipText = "Ruche n°" + pos.Key };
                    markersOverlay.Markers.Add(m);
                }
            }

            // Gestion Alerte Visuelle
            _idAlerteEnCours = -1;
            foreach (var kp in etatsAcquits) { if (!kp.Value) { _idAlerteEnCours = kp.Key; break; } }

            if (_idAlerteEnCours != -1 && messagesAlerte.ContainsKey(_idAlerteEnCours))
            {
                pnlAlerte.Visible = true;
                lblAlerte.Text = messagesAlerte[_idAlerteEnCours];
                blinkTimer?.Start();
            }
            else
            {
                pnlAlerte.Visible = false;
                blinkTimer?.Stop();
            }
        }

        private void InitInterface()
        {
            this.Text = "BEE MONITOR PRO - Unified Version";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.White;

            topPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(40, 40, 40) };

            comboRuches = new ComboBox { Location = new Point(20, 18), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            comboRuches.SelectedIndexChanged += (s, e) => {
                if (comboRuches.SelectedItem == null) return;
                _selectedId = int.Parse(comboRuches.SelectedItem.ToString().Replace("Ruche n°", ""));
                UpdateUI();
                if (positions.ContainsKey(_selectedId) && map != null) map.Position = positions[_selectedId];
            };

            // Bouton Afficher BDD (Grille)












            Button btnHist = new Button
            {
                Text = "Afficher BDD",
                Location = new Point(190, 16),
                Width = 100,
                Height = 28,
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnHist.Click += (s, e) => {
                var data = DatabaseHelper.LoadAll();
                Form f = new Form { Text = "Historique Complet", Size = new Size(900, 500), StartPosition = FormStartPosition.CenterScreen };
                DataGridView dgv = new DataGridView { Dock = DockStyle.Fill, DataSource = data, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
                f.Controls.Add(dgv);
                f.ShowDialog();
            };

            // Bouton Test BDD
            Button btnTestBDD = new Button
            {
                Text = "Test Connexion",
                Location = new Point(300, 16),
                Width = 110,
                Height = 28,
                BackColor = Color.DarkBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnTestBDD.Click += (s, e) => MessageBox.Show(DatabaseHelper.TestConnexion());

            // Bouton Recharger
            Button btnReload = new Button
            {
                Text = "Force Reload",
                Location = new Point(420, 16),
                Width = 100,
                Height = 28,
                BackColor = Color.DarkSlateGray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnReload.Click += (s, e) => ChargerHistoriqueDepuisBDD();

            topPanel.Controls.Add(comboRuches);
            topPanel.Controls.Add(btnHist);
            topPanel.Controls.Add(btnTestBDD);
            topPanel.Controls.Add(btnReload);

            pnlAlerte = new Panel { Dock = DockStyle.Top, Height = 50, Visible = false };
            lblAlerte = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), Cursor = Cursors.Hand };
            lblAlerte.Click += (s, e) => { if (_idAlerteEnCours != -1) etatsAcquits[_idAlerteEnCours] = true; UpdateUI(); };
            pnlAlerte.Controls.Add(lblAlerte);

            sidePanel = new Panel { Dock = DockStyle.Left, Width = 320, AutoScroll = true };
            sidePanel.Controls.Add(CreateModule("Humidité", Color.Teal, out lblH, out chartH));
            sidePanel.Controls.Add(CreateModule("Poids", Color.Orange, out lblP, out chartP));
            sidePanel.Controls.Add(CreateModule("Température", Color.Crimson, out lblT, out chartT));

            gridLogs = new DataGridView
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                BackgroundColor = Color.White
            };
            gridLogs.Columns.Add("H", "Heure"); gridLogs.Columns.Add("T", "Tag"); gridLogs.Columns.Add("M", "Message");

            this.Controls.Add(sidePanel);
            this.Controls.Add(gridLogs);
            this.Controls.Add(topPanel);
            this.Controls.Add(pnlAlerte);

            InitialiserLes5RuchesParDefaut();
        }

        private void InitialiserLes5RuchesParDefaut()
        {
            for (int i = 1; i <= 5; i++)
            {
                if (!histT.ContainsKey(i))
                {
                    histT[i] = new ChartValues<DateTimePoint>();
                    histP[i] = new ChartValues<DateTimePoint>();
                    histH[i] = new ChartValues<DateTimePoint>();
                    positions[i] = new PointLatLng(48.85, 2.35);
                    etatsAcquits[i] = true;
                    if (!comboRuches.Items.Contains("Ruche n°" + i)) comboRuches.Items.Add("Ruche n°" + i);
                }
            }
            if (comboRuches.Items.Count > 0) comboRuches.SelectedIndex = 0;
        }

        private void SafeInitMap()
        {
            map = new GMapControl { Dock = DockStyle.Fill, MapProvider = GMapProviders.GoogleMap, Zoom = 10, CanDragMap = true };
            markersOverlay = new GMapOverlay("markers");
            map.Overlays.Add(markersOverlay);
            this.Controls.Add(map);
            map.BringToFront();
        }

        private Panel CreateModule(string t, Color c, out Label l, out WinCharts.CartesianChart ch)
        {
            Panel p = new Panel { Dock = DockStyle.Top, Height = 200, Padding = new Padding(10) };
            l = new Label { Text = "--", Dock = DockStyle.Top, Height = 28, ForeColor = c, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
            ch = new WinCharts.CartesianChart { Dock = DockStyle.Fill, DisableAnimations = true };
            ch.Series.Add(new LineSeries
            {
                Values = new ChartValues<DateTimePoint>(),
                Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(c.R, c.G, c.B)),
                Fill = System.Windows.Media.Brushes.Transparent
            });
            p.Controls.Add(ch); p.Controls.Add(l); p.Controls.Add(new Label { Text = t, Dock = DockStyle.Top, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            return p;
        }

        private void Log(string tag, string msg, Color c)
        {
            if (this.IsDisposed || gridLogs == null) return;
            int r = gridLogs.Rows.Add(DateTime.Now.ToString("HH:mm:ss"), tag, msg);
            gridLogs.Rows[r].DefaultCellStyle.ForeColor = c;
            gridLogs.FirstDisplayedScrollingRowIndex = r;
        }

        private void AddPt(ChartValues<DateTimePoint> l, double v)
        {
            l.Add(new DateTimePoint(DateTime.Now, v));
            if (l.Count > 50) l.RemoveAt(0);
        }
    }
}