using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class Attachment
    {
        [Key] 
        public int AttachmentId { get; set; }

        [ForeignKey("Task")]
        public int? TaskId { get; set; }

        [ForeignKey("Comment")]

        public int? CommentId { get; set; }
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public DateTime DateOfUpload { get; set; }


        public Task? Task { get; set; }
        public Comment? Comment { get; set; }
    }
}
