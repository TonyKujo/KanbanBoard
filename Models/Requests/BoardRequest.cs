using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests
{
    public class BoardRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(3000)]
        public string? Description { get; set; }
    }
}
