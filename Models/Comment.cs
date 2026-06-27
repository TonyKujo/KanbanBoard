using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class Comment
    {
        [Key] 
        public int CommentId { get; set; }
        public string? Text { get; set; }
        public int AuthorId { get; set; }
        public int TaskId { get; set; }
        public bool IsEdited { get; set; }
        public DateTime DateOfMade { get; set; }

        [ForeignKey("AuthorId")]
        public BoardUser Author { get; set; } = null!;

        [ForeignKey("TaskId")]
        public Task Task { get; set; } = null!;
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    }
}
