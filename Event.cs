
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Event
    {
        public int EventID { get; set; }

        public int OrganiserID { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string EventName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        [Column(TypeName = "varchar(150)")]
        public string Location { get; set; } = string.Empty;

        [Column(TypeName = "varchar(500)")]
        public string? Description { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string EventType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(6,2)")]
        public decimal DistanceKM { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Status { get; set; } = "Upcoming";

        public Organiser? Organiser { get; set; }
    }
}