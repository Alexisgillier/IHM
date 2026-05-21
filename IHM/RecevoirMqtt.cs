using GMap.NET;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RucheMQTTApp
{
    public class RecevoirMqtt
    {
        private IMqttClient _client;

        public event Action<BeeData> OnDataReceived;
        public event Action<string, string, System.Drawing.Color> OnLog;

        public async Task Connecter()
        {
            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("eu1.cloud.thethings.network", 1883)
                .WithCredentials("my-ruche-mechain@ttn", "NNSXS.ZRR7CSCJPKW2IDCQPP37PJ3EB3AAYWIPV777J3I.ATDVLC4F36VOSPG6FGF6GXRXPPYAFRAOL2FH64B4DVXVAE4XMJCQ")
                .Build();

            _client.ApplicationMessageReceivedAsync += e =>
            {
                try
                {
                    string raw = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());

                    // LOG du message brut pour déboguer
                    OnLog?.Invoke("MQTT", "Message reçu: " + raw.Substring(0, Math.Min(100, raw.Length)), System.Drawing.Color.Blue);

                    var json = JObject.Parse(raw);
                    var decoded = json["uplink_message"]?["decoded_payload"];

                    if (decoded == null)
                    {
                        OnLog?.Invoke("WARN", "decoded_payload absent du message", System.Drawing.Color.Orange);
                        return Task.CompletedTask;
                    }

                    var data = new BeeData();
                    data.RucheId = (int)(decoded["ruche_id"] ?? 0);

                    // ── Détection du TYPE selon les champs présents ──
                    if (decoded["temperature"] != null || decoded["humidite"] != null || decoded["poids_kg"] != null)
                    {
                        // TYPE 1 : capteurs
                        data.Type = 1;
                        data.Temperature = decoded["temperature"] != null ? (double?)decoded["temperature"] : null;
                        data.Humidite = decoded["humidite"] != null ? (double?)decoded["humidite"] : null;
                        data.PoidsKg = decoded["poids_kg"] != null ? (double?)decoded["poids_kg"] : null;

                        OnLog?.Invoke("TYPE1", $"Ruche {data.RucheId} | T={data.Temperature}°C H={data.Humidite}% P={data.PoidsKg}kg", System.Drawing.Color.Green);
                    }
                    else if (decoded["latitude"] != null && decoded["longitude"] != null)
                    {
                        // TYPE 2 : GPS + vol
                        data.Type = 2;
                        data.Location = new PointLatLng((double)decoded["latitude"], (double)decoded["longitude"]);
                        data.VolAlerte = decoded["vol_en_cours"]?.ToString() ?? "NON";

                        OnLog?.Invoke("TYPE2", $"Ruche {data.RucheId} | GPS={data.Location} VOL={data.VolAlerte}", System.Drawing.Color.DodgerBlue);
                    }
                    else if (decoded["alerte_essaimage"] != null || decoded["couleur_reine"] != null)
                    {
                        // TYPE 3 : essaimage + reine
                        data.Type = 3;
                        data.AlerteEssaimage = decoded["alerte_essaimage"]?.ToString() ?? "NON";
                        data.CouleurReine = decoded["couleur_reine"]?.ToString() ?? "Inconnue";

                        OnLog?.Invoke("TYPE3", $"Ruche {data.RucheId} | ESSAIMAGE={data.AlerteEssaimage} REINE={data.CouleurReine}", System.Drawing.Color.Purple);
                    }
                    else
                    {
                        OnLog?.Invoke("WARN", $"Message non reconnu pour ruche {data.RucheId} : " + decoded.ToString(), System.Drawing.Color.Orange);
                        return Task.CompletedTask;
                    }

                    OnDataReceived?.Invoke(data);
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke("ERR", "Erreur décodage: " + ex.Message, System.Drawing.Color.Red);
                }

                return Task.CompletedTask;
            };

            await _client.ConnectAsync(options);
            await _client.SubscribeAsync("v3/+/devices/+/up");
            OnLog?.Invoke("MQTT", "Connecté et en écoute", System.Drawing.Color.Green);
        }
    }
}