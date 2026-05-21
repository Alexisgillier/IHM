using GMap.NET;

namespace RucheMQTTApp
{
    public class BeeData
    {
        public int Type { get; set; }
        public int RucheId { get; set; }
        public double? Temperature { get; set; }
        public double? Humidite { get; set; }
        public double? PoidsKg { get; set; }
        public PointLatLng? Location { get; set; }
        public string VolAlerte { get; set; }
        public string AlerteEssaimage { get; set; }
        public string CouleurReine { get; set; }
    }
}