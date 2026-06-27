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
        public int StatusId { get; set; }
        public int BoardId { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime DeadLine { get; set; }

        public BoardUser Author { get; set; } = null!;

        public BoardUser Assignee { get; set; } = null!;

        [ForeignKey("BoardId")] 
        public Board Board { get; set; } = null!;

        [ForeignKey("StatusId")] 
        public Status Status { get; set; } = null!;

        public ICollection<Comment> Comments {  get; set; } = new List<Comment> ();
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public ICollection<TaskStatusHistory> TaskStatusHistories { get; set; } = new List<TaskStatusHistory>();
    }
}
