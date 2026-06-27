using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class Task
    {
        [Key]
        public int TaskId { get; set; }
        public string TaskName { get; set; } = null!;
        public string? TaskDescription { get; set; }
        public int? AssigneeId { get; set; }
        public int AuthorId { get; set; }

        [ForeignKey("Status")]
        public int StatusId { get; set; }

        [ForeignKey("Board")]
        public int BoardId { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime DeadLine { get; set; }

        public BoardUser Author { get; set; } = null!;

        public BoardUser Assignee { get; set; } = null!;

        
        public Board Board { get; set; } = null!;

        public Status Status { get; set; } = null!;

        [InverseProperty("Task")]
        public ICollection<Comment> Comments {  get; set; } = new List<Comment> ();
        [InverseProperty("Task")]
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        [InverseProperty("Task")]
        public ICollection<TaskStatusHistory> TaskStatusHistories { get; set; } = new List<TaskStatusHistory>();
    }
}
