using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests
{
    public class CommentRequest
    {
        [Required]
        [MaxLength(10000)]
        public string Text { get; set; } = null!;
    }
}
