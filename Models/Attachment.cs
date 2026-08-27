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

        [ForeignKey("Uploader")]
        public int UploaderId { get; set; }

        [MaxLength(100)]
        public string FileName { get; set; } = null!;
        [MaxLength(1024)]
        public string FilePath { get; set; } = null!;
        public DateTime DateOfUpload { get; set; }


        public Task? Task { get; set; }
        public Comment? Comment { get; set; }
        public BoardUser Uploader { get; set; } = null!;
    }
}
