using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class Comment
    {
        [Key] 
        public int CommentId { get; set; }
        public string? Text { get; set; }
        [ForeignKey("Author")]
        public int AuthorId { get; set; }
        [ForeignKey("Task")]
        public int TaskId { get; set; }
        public bool IsEdited { get; set; }
        public DateTime DateOfMade { get; set; }

        public BoardUser Author { get; set; } = null!;

        public Task Task { get; set; } = null!;

        [InverseProperty("Comment")]
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    }
}
