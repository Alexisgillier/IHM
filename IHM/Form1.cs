using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace IHM_essai
{
    public partial class Form1 : Form
    {
        // ═══════════════════════════════════════════
        //  COULEURS
        // ═══════════════════════════════════════════
        static readonly Color BG = Color.FromArgb(26, 26, 46);
        static readonly Color CARD = Color.FromArgb(22, 33, 62);
        static readonly Color CARD2 = Color.FromArgb(15, 52, 96);
        static readonly Color HONEY = Color.FromArgb(245, 166, 35);
        static readonly Color LEAF = Color.FromArgb(64, 145, 108);
        static readonly Color MUTED = Color.FromArgb(139, 139, 154);
        static readonly Color TEXTCOLOR = Color.FromArgb(240, 230, 211);
        static readonly Color SKY = Color.FromArgb(74, 144, 217);
        static readonly Color DANGER = Color.FromArgb(230, 57, 70);

        // ═══════════════════════════════════════════
        //  MODÈLE DE DONNÉES
        // ═══════════════════════════════════════════
        class HiveData
        {
            public string Name = "";   // CS8618 : initialisé
            public string Temp = "";
            public string Hum = "";
            public string Weight = "";
            public float[] WeightSeries = Array.Empty<float>();
            public float[] TempSeries = Array.Empty<float>();
            public float[] HumSeries = Array.Empty<float>();
            public double Lat;
            public double Lng;
            public bool Stolen;
        }

        // ═══════════════════════════════════════════
        //  DONNÉES DES RUCHES
        // ═══════════════════════════════════════════
        readonly Dictionary<string, HiveData> hives = new()
        {
            ["all"] = new HiveData
            {
                Name = "Toutes les ruches",
                Temp = "Moy. 4.1 / 25.8\u00b0C",
                Hum = "87%",
                Weight = "160.2 Kg",
                WeightSeries = new float[] { 158f, 159f, 160f, 161f, 159f, 160f, 160.2f },
                TempSeries = new float[] { 24.5f, 25.1f, 25.8f, 26.2f, 25.5f, 25.9f, 25.8f },
                HumSeries = new float[] { 85f, 86f, 87f, 88f, 87f, 86f, 87f },
                Lat = 46.60,
                Lng = 2.35,
                Stolen = false
            },
            ["1"] = new HiveData
            {
                Name = "Ruche N\u00b01",
                Temp = "5.2 / 28.1\u00b0C",
                Hum = "83%",
                Weight = "31.2 Kg",
                WeightSeries = new float[] { 30.5f, 30.8f, 31f, 31.1f, 31f, 31.2f, 31.2f },
                TempSeries = new float[] { 26f, 27f, 28f, 28.1f, 27.5f, 28f, 28.1f },
                HumSeries = new float[] { 81f, 82f, 83f, 84f, 83f, 82f, 83f },
                Lat = 47.3215,
                Lng = 2.1064,
                Stolen = false
            },
            ["2"] = new HiveData
            {
                Name = "Ruche N\u00b02",
                Temp = "4.8 / 26.5\u00b0C",
                Hum = "88%",
                Weight = "28.7 Kg",
                WeightSeries = new float[] { 28f, 28.2f, 28.5f, 28.6f, 28.5f, 28.7f, 28.7f },
                TempSeries = new float[] { 25f, 25.5f, 26f, 26.5f, 26f, 26.3f, 26.5f },
                HumSeries = new float[] { 86f, 87f, 88f, 89f, 88f, 87f, 88f },
                Lat = 43.6047,
                Lng = 1.4442,
                Stolen = false
            },
            ["3"] = new HiveData
            {
                Name = "Ruche N\u00b03",
                Temp = "3.9 / 24.3\u00b0C",
                Hum = "91%",
                Weight = "35.1 Kg",
                WeightSeries = new float[] { 34.5f, 34.7f, 34.9f, 35f, 34.9f, 35.1f, 35.1f },
                TempSeries = new float[] { 23f, 23.5f, 24f, 24.3f, 24f, 24.2f, 24.3f },
                HumSeries = new float[] { 89f, 90f, 91f, 92f, 91f, 90f, 91f },
                Lat = 48.8566,
                Lng = 2.3522,
                Stolen = false
            },
            ["4"] = new HiveData
            {
                Name = "Ruche N\u00b04",
                Temp = "6.1 / 29.0\u00b0C",
                Hum = "85%",
                Weight = "32.4 Kg",
                WeightSeries = new float[] { 31.8f, 32f, 32.2f, 32.3f, 32.2f, 32.4f, 32.4f },
                TempSeries = new float[] { 27f, 28f, 28.5f, 29f, 28.5f, 28.8f, 29f },
                HumSeries = new float[] { 83f, 84f, 85f, 86f, 85f, 84f, 85f },
                Lat = 45.7640,
                Lng = 4.8357,
                Stolen = false
            },
            ["5"] = new HiveData
            {
                Name = "Ruche N\u00b05",
                Temp = "3.5 / 27.2\u00b0C",
                Hum = "90%",
                Weight = "33.8 Kg",
                WeightSeries = new float[] { 33f, 33.2f, 33.5f, 33.6f, 33.5f, 33.8f, 33.8f },
                TempSeries = new float[] { 25.5f, 26f, 26.8f, 27.2f, 27f, 27.1f, 27.2f },
                HumSeries = new float[] { 88f, 89f, 90f, 91f, 90f, 89f, 90f },
                Lat = 44.8378,
                Lng = -0.5792,
                Stolen = false
            },
        };

        // ═══════════════════════════════════════════
        //  CHAMPS
        // ═══════════════════════════════════════════
        string currentKey = "all";
        string currentChart = "weight"; // "weight" | "temp" | "hum"

        // Contrôles — initialisés dans BuildUI() avant tout usage
        ComboBox cboHive = null!;
        Label lblClock = null!;
        Label lblChartTitle = null!;
        Panel pnlChart = null!;
        Panel pnlStats = null!;
        WebView2 webMap = null!;
        System.Windows.Forms.Timer ticker = null!;

        // Heure en ligne
        static readonly HttpClient httpClient = new();
        DateTime onlineBase = DateTime.MinValue;
        DateTime localSnapshot = DateTime.MinValue;
        bool timeReady = false;

        // ═══════════════════════════════════════════
        //  CONSTRUCTEUR
        // ═══════════════════════════════════════════
        public Form1()
        {
            InitializeComponent();
            BuildUI();
            Load += OnFormLoad;
        }

        async void OnFormLoad(object? sender, EventArgs e)
        {
            await webMap.EnsureCoreWebView2Async(null);
            LoadMap();
        }

        // ═══════════════════════════════════════════
        //  CONSTRUCTION INTERFACE
        // ═══════════════════════════════════════════
        void BuildUI()
        {
            Text = "BeeMonitor";
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1000, 650);
            BackColor = BG;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 215f));
            Controls.Add(root);

            root.Controls.Add(MakeHeader(), 0, 0);
            root.Controls.Add(MakeMap(), 0, 1);
            root.Controls.Add(MakeBottom(), 0, 2);

            ticker = new System.Windows.Forms.Timer { Interval = 1000 };
            ticker.Tick += OnTick;
            ticker.Start();

            SyncOnlineTime();
            OnTick(this, EventArgs.Empty);
        }

        // ─── HEADER ─────────────────────────────────
        Panel MakeHeader()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = CARD2 };
            pnl.Paint += (object? s, PaintEventArgs e) =>
            {
                using Pen p = new(HONEY, 2);
                e.Graphics.DrawLine(p, 0, pnl.Height - 1, pnl.Width, pnl.Height - 1);
            };

            var bee = new Label
            {
                Text = "🐝",
                Font = new Font("Segoe UI Emoji", 13f),
                Left = 10,
                Top = 24,
                Width = 28,
                Height = 28,
                AutoSize = false,
                BackColor = Color.Transparent
            };

            var title = new Label
            {
                Text = "BeeMonitor",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Left = 44,
                Top = 10,
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = HONEY
            };

            var sub = new Label
            {
                Text = "Surveillance connect\u00e9e des ruches",
                Font = new Font("Courier New", 8f),
                Left = 46,
                Top = 42,
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = MUTED
            };

            lblClock = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = true,
                Top = 28,
                BackColor = Color.Transparent,
                ForeColor = TEXTCOLOR
            };

            var lblSel = new Label
            {
                Text = "🏠 Ruche :",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Top = 29,
                BackColor = Color.Transparent,
                ForeColor = HONEY
            };

            cboHive = new ComboBox
            {
                Width = 160,
                Height = 28,
                Top = 24,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = CARD2,
                ForeColor = TEXTCOLOR,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            cboHive.Items.AddRange(new object[]
                { "Toutes (5)", "Ruche N\u00b01", "Ruche N\u00b02",
                  "Ruche N\u00b03", "Ruche N\u00b04", "Ruche N\u00b05" });
            cboHive.SelectedIndex = 0;
            cboHive.SelectedIndexChanged += OnHiveChanged;

            pnl.Resize += (object? s, EventArgs e) =>
            {
                cboHive.Left = pnl.Width - cboHive.Width - 14;
                lblSel.Left = cboHive.Left - lblSel.PreferredWidth - 8;
                lblClock.Left = (pnl.Width - lblClock.PreferredWidth) / 2;
            };

            pnl.Controls.Add(bee);
            pnl.Controls.Add(title);
            pnl.Controls.Add(sub);
            pnl.Controls.Add(lblClock);
            pnl.Controls.Add(lblSel);
            pnl.Controls.Add(cboHive);
            return pnl;
        }

        // ─── SYNCHRO HEURE EN LIGNE ─────────────────
        async void SyncOnlineTime()
        {
            string[] urls =
            {
                "https://timeapi.io/api/Time/current/zone?timeZone=Europe%2FParis",
                "https://worldtimeapi.org/api/timezone/Europe/Paris",
            };

            foreach (string url in urls)
            {
                try
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "BeeMonitor/1.0");

                    var resp = await httpClient.GetAsync(url);
                    if (!resp.IsSuccessStatusCode) continue;
                    string json = await resp.Content.ReadAsStringAsync();

                    DateTime parsed = DateTime.MinValue;

                    if (url.Contains("timeapi.io"))
                    {
                        int idx = json.IndexOf("\"dateTime\":");
                        if (idx >= 0)
                        {
                            int s2 = json.IndexOf('"', idx + 11) + 1;
                            int e2 = json.IndexOf('"', s2);
                            parsed = DateTime.Parse(
                                json[s2..e2],
                                CultureInfo.InvariantCulture);
                        }
                    }
                    else if (url.Contains("worldtimeapi"))
                    {
                        int idx = json.IndexOf("\"datetime\":");
                        if (idx >= 0)
                        {
                            int s2 = json.IndexOf('"', idx + 11) + 1;
                            int e2 = json.IndexOf('"', s2);
                            parsed = DateTime.Parse(
                                json[s2..e2],
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.RoundtripKind).ToLocalTime();
                        }
                    }

                    if (parsed != DateTime.MinValue)
                    {
                        onlineBase = parsed;
                        localSnapshot = DateTime.Now;
                        timeReady = true;

                        if (lblClock.IsHandleCreated)
                            lblClock.Invoke(() => OnTick(this, EventArgs.Empty));

                        break;
                    }
                }
                catch { /* essai suivant */ }
            }

            await Task.Delay(5 * 60 * 1000);
            SyncOnlineTime();
        }

        void OnTick(object? sender, EventArgs e)
        {
            if (!timeReady)
            {
                lblClock.Text = "⏳  Synchronisation de l'heure...";
                lblClock.ForeColor = MUTED;
                if (lblClock.Parent != null)
                    lblClock.Left = (lblClock.Parent.Width - lblClock.PreferredWidth) / 2;
                return;
            }

            TimeSpan elapsed = DateTime.Now - localSnapshot;
            DateTime now = onlineBase + elapsed;

            lblClock.ForeColor = TEXTCOLOR;
            var fr = new CultureInfo("fr-FR");
            string jour = now.ToString("dddd", fr);
            jour = char.ToUpper(jour[0]) + jour[1..];
            lblClock.Text =
                jour + " " + now.Day + " " +
                now.ToString("MMMM yyyy", fr) + "   \u2022   " +
                now.ToString("HH:mm:ss");

            if (lblClock.Parent != null)
                lblClock.Left = (lblClock.Parent.Width - lblClock.PreferredWidth) / 2;
        }

        // ─── CARTE ──────────────────────────────────
        Panel MakeMap()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                Padding = new Padding(10, 8, 10, 4)
            };
            webMap = new WebView2 { Dock = DockStyle.Fill };
            pnl.Controls.Add(webMap);
            return pnl;
        }

        // ─── BAS ────────────────────────────────────
        Panel MakeBottom()
        {
            var outer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                Padding = new Padding(10, 6, 10, 8)
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                ColumnCount = 2,
                RowCount = 1
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            outer.Controls.Add(tbl);

            // ── Graphique ──
            var chartCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CARD,
                Margin = new Padding(0, 0, 8, 0)
            };
            chartCard.Paint += (object? s, PaintEventArgs e) =>
            {
                using Pen p = new(Color.FromArgb(20, 255, 255, 255), 1);
                RoundRect(e.Graphics, p,
                    new Rectangle(0, 0, chartCard.Width - 1, chartCard.Height - 1), 10);
            };

            lblChartTitle = new Label
            {
                Text = "\u23f8  \u00c9volution du poids \u2014 7 derniers jours",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Left = 12,
                Top = 8,
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = MUTED
            };

            pnlChart = new Panel
            {
                Left = 8,
                Top = 30,
                BackColor = Color.Transparent
            };
            pnlChart.Paint += DrawChart;

            chartCard.Resize += (object? s, EventArgs e) =>
            {
                pnlChart.Width = chartCard.ClientSize.Width - 16;
                pnlChart.Height = Math.Max(10, chartCard.ClientSize.Height - 38);
                pnlChart.Invalidate();
            };

            chartCard.Controls.Add(lblChartTitle);
            chartCard.Controls.Add(pnlChart);
            tbl.Controls.Add(chartCard, 0, 0);

            // ── Stats ──
            pnlStats = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                Margin = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            pnlStats.Paint += DrawStats;
            pnlStats.Resize += (object? s, EventArgs e) => pnlStats.Invalidate();
            pnlStats.MouseClick += OnStatClick;
            tbl.Controls.Add(pnlStats, 1, 0);

            return outer;
        }

        // ═══════════════════════════════════════════
        //  CLIC SUR UNE CARTE STAT
        // ═══════════════════════════════════════════
        void OnStatClick(object? sender, MouseEventArgs e)
        {
            int gap = 10;
            int cW = (pnlStats.Width - gap * 2) / 3;

            string[] keys = { "temp", "hum", "weight" };
            string[] titles =
            {
                "🌡\ufe0f  Temp\u00e9rature \u2014 7 derniers jours",
                "💧  Humidit\u00e9 \u2014 7 derniers jours",
                "\u2696\ufe0f  Poids \u2014 7 derniers jours"
            };

            for (int i = 0; i < 3; i++)
            {
                int cx = i * (cW + gap);
                if (e.X >= cx && e.X < cx + cW)
                {
                    currentChart = keys[i];
                    lblChartTitle.Text = titles[i];
                    pnlStats.Invalidate();
                    pnlChart.Invalidate();
                    break;
                }
            }
        }

        // ═══════════════════════════════════════════
        //  CARTE LEAFLET
        // ═══════════════════════════════════════════
        void LoadMap()
        {
            if (webMap.CoreWebView2 == null) return;
            webMap.CoreWebView2.NavigateToString(BuildHtml());
        }

        string BuildHtml()
        {
            if (!hives.TryGetValue(currentKey, out HiveData? d))
                d = hives["all"];

            string latC = d.Lat.ToString(CultureInfo.InvariantCulture);
            string lngC = d.Lng.ToString(CultureInfo.InvariantCulture);
            int zoom = (currentKey == "all") ? 6 : 12;

            var jsData = new System.Text.StringBuilder();
            jsData.AppendLine("var hiveInfo = {");
            foreach (KeyValuePair<string, HiveData> kv in hives)
            {
                if (kv.Key == "all") continue;
                HiveData h = kv.Value;
                string lat = h.Lat.ToString(CultureInfo.InvariantCulture);
                string lng = h.Lng.ToString(CultureInfo.InvariantCulture);
                string st = h.Stolen ? "true" : "false";
                jsData.AppendLine(
                    $"  '{kv.Key}':{{name:'{h.Name}',temp:'{h.Temp}',hum:'{h.Hum}'," +
                    $"weight:'{h.Weight}',lat:{lat},lng:{lng},stolen:{st}}},");
            }
            jsData.AppendLine("};");

            string filter = (currentKey == "all") ? "null" : $"'{currentKey}'";

            string html =
"<!DOCTYPE html>" +
"<html><head><meta charset='utf-8'/>" +
"<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>" +
"<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>" +
"<style>" +
"*{margin:0;padding:0;box-sizing:border-box;}" +
"html,body,#map{width:100%;height:100%;}" +
".leaflet-popup-content-wrapper{background:#0F3460;color:#F0E6D3;border:2px solid #F5A623;border-radius:12px;font-family:'Segoe UI',sans-serif;}" +
".leaflet-popup-tip{background:#0F3460;}" +
".leaflet-popup-content{margin:14px 18px;min-width:210px;}" +
".vbtn{margin-top:10px;width:100%;padding:8px 0;border:none;border-radius:8px;font-size:13px;font-weight:bold;cursor:pointer;font-family:'Segoe UI',sans-serif;}" +
"</style>" +
"</head><body>" +
"<div id='map'></div>" +
"<script>" +
jsData.ToString() +
$"var filter={filter};" +
"var markers={};" +
"function makeIcon(stolen){" +
"  var col=stolen?'#E63946':'#40916C';" +
"  var em=stolen?'🚨':'🐝';" +
"  return L.divIcon({className:'',html:'<div style=\"width:40px;height:40px;background:'+col+';border-radius:50%;border:3px solid white;display:flex;align-items:center;justify-content:center;font-size:20px;box-shadow:0 0 14px '+col+'88;\">'+em+'</div>',iconSize:[40,40],iconAnchor:[20,20],popupAnchor:[0,-24]});}" +
"function makePopup(k){" +
"  var h=hiveInfo[k];" +
"  var col=h.stolen?'#E63946':'#40916C';" +
"  var warn=h.stolen?'<div style=\"color:#E63946;font-weight:bold;margin:4px 0;\">⚠️ VOL SIGNAL\u00c9</div>':'';" +
"  var bc=h.stolen?'#40916C':'#E63946';" +
"  var bt=h.stolen?'✅ Annuler le signalement':'🚨 Signaler un vol';" +
"  return '<b style=\"font-size:15px;color:'+col+'\">'+h.name+'</b>'+warn+'<br>🌡️ <b>Temp :</b> '+h.temp+'<br>💧 <b>Humidit\u00e9 :</b> '+h.hum+'<br>⚖️ <b>Poids :</b> '+h.weight+'<br><button class=\"vbtn\" style=\"background:'+bc+';color:white;\" onclick=\"toggleVol(\\''+k+'\\');\">'+bt+'</button>';}" +
"function toggleVol(k){" +
"  hiveInfo[k].stolen=!hiveInfo[k].stolen;" +
"  markers[k].setIcon(makeIcon(hiveInfo[k].stolen));" +
"  markers[k].setPopupContent(makePopup(k));" +
"  markers[k].openPopup();}" +
$"var map=L.map('map').setView([{latC},{lngC}],{zoom});" +
"L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',{attribution:'\u00a9 <a href=\"https://openstreetmap.org\">OpenStreetMap</a> contributors',maxZoom:19}).addTo(map);" +
"for(var key in hiveInfo){" +
"  if(filter!==null && key!==filter) continue;" +
"  (function(k){" +
"    var m=L.marker([hiveInfo[k].lat,hiveInfo[k].lng],{icon:makeIcon(hiveInfo[k].stolen)}).addTo(map).bindPopup(makePopup(k));" +
"    markers[k]=m;" +
"  })(key);}" +
"</script></body></html>";

            return html;
        }

        // ═══════════════════════════════════════════
        //  GRAPHIQUE
        // ═══════════════════════════════════════════
        void DrawChart(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int W = pnlChart.Width, H = pnlChart.Height;
            if (W < 50 || H < 50) return;

            int pL = 54, pR = 70, pT = 20, pB = 34;
            int dW = W - pL - pR, dH = H - pT - pB;
            if (dW < 10 || dH < 10) return;

            if (!hives.TryGetValue(currentKey, out HiveData? data))
                data = hives["all"];

            float[] series;
            float[] avgSeries;
            Color cc;
            string unit;
            string seriesLabel;

            if (currentChart == "temp")
            {
                series = data.TempSeries;
                avgSeries = new float[] { 24.8f, 25.0f, 25.5f, 25.9f, 25.4f, 25.7f, 25.8f };
                cc = DANGER;
                unit = "\u00b0C";
                seriesLabel = "Temp\u00e9rature";
            }
            else if (currentChart == "hum")
            {
                series = data.HumSeries;
                avgSeries = new float[] { 86f, 87f, 87.5f, 88f, 87.5f, 87f, 87f };
                cc = SKY;
                unit = "%";
                seriesLabel = "Humidit\u00e9";
            }
            else
            {
                series = data.WeightSeries;
                avgSeries = new float[] { 31.5f, 31.7f, 32f, 32.1f, 32f, 32.2f, 32.2f };
                bool stolen = currentKey != "all" &&
                              hives.TryGetValue(currentKey, out HiveData? hv) &&
                              hv!.Stolen;
                cc = stolen ? DANGER : LEAF;
                unit = " Kg";
                seriesLabel = "Poids";
            }

            // 7 dernières dates
            var fr = new CultureInfo("fr-FR");
            string[] labels = new string[7];
            for (int i = 0; i < 7; i++)
                labels[i] = DateTime.Today.AddDays(i - 6).ToString("dd/MM", fr);

            // Min / Max
            float mn = float.MaxValue, mx = float.MinValue;
            foreach (float v in series) { if (v < mn) mn = v; if (v > mx) mx = v; }
            foreach (float v in avgSeries) { if (v < mn) mn = v; if (v > mx) mx = v; }
            float rng = mx - mn; if (rng < 0.1f) rng = 1f;
            mn -= rng * 0.15f; mx += rng * 0.15f; rng = mx - mn;

            using Font smallFont = new("Segoe UI", 7.5f);
            using Pen gridPen = new(Color.FromArgb(22, 255, 255, 255), 1);

            for (int i = 0; i <= 4; i++)
            {
                float v = mn + rng * i / 4f;
                float gy = pT + dH - dH * (float)i / 4f;
                g.DrawLine(gridPen, pL, gy, pL + dW, gy);
                using SolidBrush sb = new(MUTED);
                g.DrawString(v.ToString("F1") + unit, smallFont, sb, 2, gy - 8);
            }
            using (Pen axisP = new(Color.FromArgb(40, 255, 255, 255), 1))
                g.DrawLine(axisP, pL, pT, pL, pT + dH);

            // Points
            PointF[] pts = new PointF[series.Length];
            PointF[] avgPts = new PointF[avgSeries.Length];
            for (int i = 0; i < series.Length; i++)
                pts[i] = new PointF(
                    pL + (float)i / (series.Length - 1) * dW,
                    pT + dH - (series[i] - mn) / rng * dH);
            for (int i = 0; i < avgSeries.Length; i++)
                avgPts[i] = new PointF(
                    pL + (float)i / (avgSeries.Length - 1) * dW,
                    pT + dH - (avgSeries[i] - mn) / rng * dH);

            // Zone remplie
            PointF[] fillPts = new PointF[series.Length + 2];
            for (int i = 0; i < series.Length; i++) fillPts[i] = pts[i];
            fillPts[series.Length] = new PointF(pL + dW, pT + dH);
            fillPts[series.Length + 1] = new PointF(pL, pT + dH);
            using (SolidBrush fb = new(Color.FromArgb(45, cc)))
                g.FillPolygon(fb, fillPts);

            // Courbe principale
            using (Pen lp = new(cc, 2.5f))
                g.DrawCurve(lp, pts, 0.35f);
            foreach (PointF pt in pts)
            {
                using SolidBrush outer2 = new(cc);
                using SolidBrush inner = new(CARD);
                g.FillEllipse(outer2, pt.X - 5, pt.Y - 5, 10, 10);
                g.FillEllipse(inner, pt.X - 2, pt.Y - 2, 4, 4);
            }

            // Courbe moyenne (tirets)
            using (Pen dashPen = new(MUTED, 1.5f) { DashStyle = DashStyle.Dash })
                g.DrawCurve(dashPen, avgPts, 0.35f);

            // Labels X
            using Font dateFont = new("Segoe UI", 7.5f);
            for (int i = 0; i < labels.Length; i++)
            {
                SizeF sz = g.MeasureString(labels[i], dateFont);
                using SolidBrush tb = new(TEXTCOLOR);
                g.DrawString(labels[i], dateFont, tb, pts[i].X - sz.Width / 2, pT + dH + 5);
            }

            // Valeur finale
            using Font boldFont = new("Segoe UI", 8.5f, FontStyle.Bold);
            using SolidBrush valBrush = new(cc);
            g.DrawString(
                series[^1].ToString("F1") + unit,
                boldFont, valBrush,
                pts[^1].X + 8, pts[^1].Y - 12);

            // Légende
            using SolidBrush leg1 = new(cc);
            using SolidBrush leg2 = new(MUTED);
            g.FillRectangle(leg1, pL + 4, pT + 2, 12, 12);
            g.DrawString(seriesLabel, smallFont, leg2, pL + 18, pT + 2);
            g.FillRectangle(leg2, pL + 4, pT + 16, 12, 12);
            g.DrawString("Moyenne", smallFont, leg2, pL + 18, pT + 16);
        }

        // ═══════════════════════════════════════════
        //  STATS
        // ═══════════════════════════════════════════
        void DrawStats(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int W = pnlStats.Width, H = pnlStats.Height;
            if (W < 30 || H < 30) return;

            if (!hives.TryGetValue(currentKey, out HiveData? d))
                d = hives["all"];

            float hp = 0.87f;
            if (float.TryParse(
                    d.Hum.Replace("%", "").Trim(),
                    NumberStyles.Float, CultureInfo.InvariantCulture,
                    out float parsedHp))
                hp = parsedHp / 100f;

            string[] labels = { "Temp\u00e9rature", "Humidit\u00e9", "Poids total" };
            string[] icons = { "🌡\ufe0f", "💧", "\u2696\ufe0f" };
            string[] values = { d.Temp, d.Hum, d.Weight };
            Color[] colors = { DANGER, SKY, HONEY };
            float[] pcts = { 0.72f, hp, 0.67f };
            string[] ckeys = { "temp", "hum", "weight" };

            int gap = 10;
            int cW = (W - gap * 2) / 3;

            for (int i = 0; i < 3; i++)
            {
                int cx = i * (cW + gap);
                Color col = colors[i];
                bool selected = currentChart == ckeys[i];
                var rect = new Rectangle(cx, 0, cW, H);

                Color bgCol = selected
                    ? Color.FromArgb(35, 55, 90)
                    : CARD;
                RoundRect(g, null, rect, 12, new SolidBrush(bgCol));

                using Pen borderPen = selected
                    ? new Pen(col, 2.5f)
                    : new Pen(Color.FromArgb(60, col), 1.5f);
                RoundRect(g, borderPen, rect, 12);

                using SolidBrush topBar = new(col);
                RoundRect(g, null, new Rectangle(cx + 2, 2, cW - 4, 6), 3, topBar);

                if (selected)
                {
                    using Font sf = new("Segoe UI", 7.5f, FontStyle.Bold);
                    const string at = "\u25cf Actif";
                    SizeF as2 = g.MeasureString(at, sf);
                    using SolidBrush ab = new(col);
                    g.DrawString(at, sf, ab, cx + cW - (int)as2.Width - 8, 8);
                }

                using Font iconFont = new("Segoe UI Emoji", 22f);
                using SolidBrush ib = new(col);
                g.DrawString(icons[i], iconFont, ib, cx + 14, 12);

                float vs = 18f;
                while (vs > 10f)
                {
                    using Font tf = new("Segoe UI", vs, FontStyle.Bold);
                    if (g.MeasureString(values[i], tf).Width <= cW - 28) break;
                    vs -= 1f;
                }
                using Font valFont = new("Segoe UI", vs, FontStyle.Bold);
                int vy = H / 2 - 2;
                using SolidBrush vb = new(TEXTCOLOR);
                g.DrawString(values[i], valFont, vb, cx + 14, vy);

                using Font lblFont = new("Segoe UI", 9f);
                int ly = Math.Min(
                    vy + (int)g.MeasureString(values[i], valFont).Height + 2,
                    H - 36);
                using SolidBrush lb2 = new(MUTED);
                g.DrawString(labels[i], lblFont, lb2, cx + 14, ly);

                int by = H - 16, bw = cW - 28;
                if (bw > 0)
                {
                    using SolidBrush trackBrush = new(Color.FromArgb(25, 255, 255, 255));
                    g.FillRectangle(trackBrush, cx + 14, by, bw, 7);
                    using LinearGradientBrush bb = new(
                        new Rectangle(cx + 14, by, bw, 7),
                        col, Color.FromArgb(160, col),
                        LinearGradientMode.Horizontal);
                    g.FillRectangle(bb, cx + 14, by, (int)(bw * pcts[i]), 7);
                }
            }
        }

        // ═══════════════════════════════════════════
        //  ÉVÉNEMENTS
        // ═══════════════════════════════════════════
        void OnHiveChanged(object? sender, EventArgs e)
        {
            string[] keys = { "all", "1", "2", "3", "4", "5" };
            currentKey = keys[cboHive.SelectedIndex];
            LoadMap();
            pnlChart.Invalidate();
            pnlStats.Invalidate();
        }

        // ═══════════════════════════════════════════
        //  HELPER DESSIN ARRONDI
        // ═══════════════════════════════════════════
        static void RoundRect(Graphics g, Pen? pen, Rectangle r, int rad, Brush? fill = null)
        {
            using GraphicsPath path = new();
            path.AddArc(r.X, r.Y, rad * 2, rad * 2, 180, 90);
            path.AddArc(r.Right - rad * 2, r.Y, rad * 2, rad * 2, 270, 90);
            path.AddArc(r.Right - rad * 2, r.Bottom - rad * 2, rad * 2, rad * 2, 0, 90);
            path.AddArc(r.X, r.Bottom - rad * 2, rad * 2, rad * 2, 90, 90);
            path.CloseAllFigures();
            if (fill != null) g.FillPath(fill, path);
            if (pen != null) g.DrawPath(pen, path);
        }
    }
}