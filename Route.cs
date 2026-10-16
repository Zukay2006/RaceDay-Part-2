
namespace RaceDay.API.Models
{
    public class Route
    {
        public int RouteID { get; set; }

        public int EventID { get; set; }

        public string RouteName { get; set; } = string.Empty;

        public decimal DistanceKM { get; set; }

        public string StartPoint { get; set; } = string.Empty;

        public string FinishPoint { get; set; } = string.Empty;

        public string RouteDescription { get; set; } = string.Empty;

        public Event? Event { get; set; }
    }
}