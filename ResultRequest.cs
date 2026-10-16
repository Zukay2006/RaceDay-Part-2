
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class ResultRequest
    {
        public TimeSpan? FinishTime { get; set; }

        [Range(1, int.MaxValue)]
        public int? Position { get; set; }

        [Required]
        [StringLength(20)]
        public string ResultStatus { get; set; } = "Pending";
    }
}