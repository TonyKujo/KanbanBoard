using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class Comment
    {
        public Comment () { DateOfMade = DateTime.UtcNow; }
        [Key] public int CommentId { get; set; }
        public string Text { get; set; }
        public int AuthorId { get; set; }
        public int TaskId { get; set; }
        public DateTime DateOfMade { get; set; }

        [ForeignKey("AuthorId")]
        public BoardUser Author { get; set; }
        [ForeignKey("TaskId")]
        public Task Task { get; set; }
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    }
}
