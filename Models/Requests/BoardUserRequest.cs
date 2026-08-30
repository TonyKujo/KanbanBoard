using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests
{
    public class BoardUserRequest
    {
        [Required]
        [MaxLength(100)]
        public string Login { get; set; } = null!;
    }
}
