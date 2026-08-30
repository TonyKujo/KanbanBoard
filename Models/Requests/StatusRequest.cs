using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests
{
    public class StatusRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;
    }
}
