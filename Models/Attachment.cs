using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class Attachment
    {
        [Key] 
        public int AttachmentId { get; set; }
        public int? TaskId { get; set; }
        public int? CommentId { get; set; }
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public DateTime DateOfUpload { get; set; }

        [ForeignKey("TaskId")] 
        public Task? Task { get; set; }

        [ForeignKey("CommentId")] 
        public Comment? Comment { get; set; }
    }
}
