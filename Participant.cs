
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Participant
    {
        public int ParticipantID { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Column("Password")]
        public string PasswordHash { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
    }
}