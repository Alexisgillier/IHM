using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using GMap.NET;

namespace RucheMQTTApp
{
    // STRUCTURE POUR LA BASE DE DONNÉES
    public class Mesure
    {
        public int Id { get; set; }
        public int RucheId { get; set; }
        public DateTime DateMesure { get; set; }
        public double? Temp { get; set; }
        public double? Humid { get; set; }
        public double? Poids { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string VolEnCours { get; set; }
        public string AlerteEssaimage { get; set; }
        public string CouleurReine { get; set; }
    }

    public static class DatabaseHelper
    {
        // CONFIGURATION
        private static string server = "192.168.1.101";
        private static string port = "3306";
        private static string database = "ruche";
        private static string user = "user";
        private static string password = "Laon2026";

        private static string ConnString =>
            $"Server={server};Port={port};Database={database};Uid={user};Pwd={password};Charset=utf8;SslMode=None;";

        private static readonly object _lock = new object();

        /// <summary>
        /// Teste si la connexion au serveur MySQL fonctionne
        /// </summary>
        public static string TestConnexion()
        {
            try
            {
                using (var conn = new MySqlConnection(ConnString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM mesure", conn))
                    {
                        object count = cmd.ExecuteScalar();
                        return $"✅ CONNEXION OK !\n\nServeur : {server}\nBase : {database}\nLignes trouvées : {count}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"❌ ERREUR DE CONNEXION :\n\n{ex.Message}\n\nVérifiez l'IP {server} et si MySQL est lancé.";
            }
        }

        /// <summary>
        /// Charge TOUTES les données pour le diagnostic et l'historique
        /// </summary>
        public static List<Mesure> LoadAll()
        {
            var list = new List<Mesure>();
            lock (_lock)
            {
                try
                {
                    using (var conn = new MySqlConnection(ConnString))
                    {
                        conn.Open();
                        string sql = "SELECT * FROM mesure ORDER BY id DESC";
                        using (var cmd = new MySqlCommand(sql, conn))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    list.Add(ReadMesure(reader));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[BDD] ERREUR LECTURE : " + ex.Message);
                }
            }
            return list;
        }

        // --- MÉTHODES DE SAUVEGARDE (MQTT) ---

        public static void SaveBeeData(BeeData data)
        {
            if (data == null) return;
            switch (data.Type)
            {
                case 1: SaveType1(data.RucheId, data.Temperature ?? 0, data.Humidite ?? 0, data.PoidsKg ?? 0); break;
                case 2: SaveType2(data.RucheId, data.Location?.Lat ?? 0, data.Location?.Lng ?? 0, data.VolAlerte); break;
                case 3: SaveType3(data.RucheId, data.AlerteEssaimage, data.CouleurReine); break;
            }
        }

        private static void SaveType1(int rid, double t, double h, double p)
        {
            ExecuteNonQuery("INSERT INTO mesure (ruche_id, temperature, humidite, poids_kg) VALUES (@rid, @t, @h, @p)",
                cmd => {
                    cmd.Parameters.AddWithValue("@rid", rid);
                    cmd.Parameters.AddWithValue("@t", t);
                    cmd.Parameters.AddWithValue("@h", h);
                    cmd.Parameters.AddWithValue("@p", p);
                });
        }

        private static void SaveType2(int rid, double lat, double lon, string vol)
        {
            ExecuteNonQuery("INSERT INTO mesure (ruche_id, latitude, longitude, vol_en_cours) VALUES (@rid, @lat, @lon, @vol)",
                cmd => {
                    cmd.Parameters.AddWithValue("@rid", rid);
                    cmd.Parameters.AddWithValue("@lat", lat);
                    cmd.Parameters.AddWithValue("@lon", lon);
                    cmd.Parameters.AddWithValue("@vol", vol);
                });
        }

        private static void SaveType3(int rid, string ess, string reine)
        {
            ExecuteNonQuery("INSERT INTO mesure (ruche_id, alerte_essaimage, couleur_reine) VALUES (@rid, @ess, @reine)",
                cmd => {
                    cmd.Parameters.AddWithValue("@rid", rid);
                    cmd.Parameters.AddWithValue("@ess", ess);
                    cmd.Parameters.AddWithValue("@reine", reine);
                });
        }

        private static void ExecuteNonQuery(string query, Action<MySqlCommand> paramAction)
        {
            lock (_lock)
            {
                try
                {
                    using (var conn = new MySqlConnection(ConnString))
                    {
                        conn.Open();
                        using (var cmd = new MySqlCommand(query, conn))
                        {
                            paramAction(cmd);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine("[BDD] Save error: " + ex.Message); }
            }
        }

        private static Mesure ReadMesure(System.Data.Common.DbDataReader r)
        {
            return new Mesure
            {
                Id = Convert.ToInt32(r["id"]),
                RucheId = Convert.ToInt32(r["ruche_id"]),
                DateMesure = Convert.ToDateTime(r["date_mesure"]),
                Temp = r["temperature"] == DBNull.Value ? null : (double?)Convert.ToDouble(r["temperature"]),
                Humid = r["humidite"] == DBNull.Value ? null : (double?)Convert.ToDouble(r["humidite"]),
                Poids = r["poids_kg"] == DBNull.Value ? null : (double?)Convert.ToDouble(r["poids_kg"]),
                Latitude = r["latitude"] == DBNull.Value ? null : (double?)Convert.ToDouble(r["latitude"]),
                Longitude = r["longitude"] == DBNull.Value ? null : (double?)Convert.ToDouble(r["longitude"]),
                VolEnCours = r["vol_en_cours"]?.ToString(),
                AlerteEssaimage = r["alerte_essaimage"]?.ToString(),
                CouleurReine = r["couleur_reine"]?.ToString()
            };
        }
    }
}