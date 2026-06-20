using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class TaskStatusHistory
    {
        public TaskStatusHistory() { ChangeDate = DateTime.UtcNow; }

        [Key] public int StatusChangeId { get; set; }
        public int TaskId { get; set; }
        public int StatusId { get; set; }
        public int AuthorId { get; set; }
        public DateTime ChangeDate { get; set; }

        [ForeignKey("TaskId")] public Task Task { get; set; }
        [ForeignKey("StatusId")] public Status Status { get; set; }
        [ForeignKey("AuthorId")] public BoardUser Author { get; set; }
    }
}