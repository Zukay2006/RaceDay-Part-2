
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Enrolment
    {
        public int EnrolmentID { get; set; }

        public int ParticipantID { get; set; }

        public int CategoryID { get; set; }

        [Column(TypeName = "date")]
        public DateTime EnrolmentDate { get; set; } = DateTime.Today;

        [Column(TypeName = "varchar(20)")]
        public string EnrolmentStatus { get; set; } = "Active";

        public Participant? Participant { get; set; }

        public Category? Category { get; set; }

        public Result? Result { get; set; }
    }
}
