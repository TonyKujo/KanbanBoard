using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests
{
    public class TaskRequest
    {
        [Required]
        [MaxLength(50)]
        public string TaskName { get; set; } = null!;

        [MaxLength(3000)]
        public string? TaskDescription { get; set; }

        public DateTime Deadline { get; set; }

        public int WorkerId { get; set; }
    }
}
