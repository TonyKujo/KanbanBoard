using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class Task
    {
        public Task () { DateOfMade = DateTime.UtcNow; }
        [Key] public int TaskId { get; set; }
        public string TaskName { get; set; }
        public string TaskDescription { get; set; }
        public int? AssigneeId { get; set; }
        public int AuthorId { get; set; }
        public int StatusId { get; set; }
        public int BoardId { get; set; }
        public DateTime DateOfMade { get; set; }

        [InverseProperty("AuthoredTasks")] public BoardUser Author { get; set; }
        [InverseProperty("AssignedTasks")] public BoardUser Assignee { get; set; }
        [ForeignKey("BoardId")] public Board Board { get; set; }
        [ForeignKey("StatusId")] public Status Status { get; set; }
        public ICollection<Comment> Comments {  get; set; } = new List<Comment> ();
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public ICollection<TaskStatusHistory> TaskStatusHistories { get; set; } = new List<TaskStatusHistory>();
    }
}
