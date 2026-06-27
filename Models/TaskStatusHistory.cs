using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class TaskStatusHistory
    {
        [Key] 
        public int StatusChangeId { get; set; }
        public int TaskId { get; set; }
        public int StatusId { get; set; }
        public int AuthorId { get; set; }
        public DateTime ChangeDate { get; set; }

        [ForeignKey("TaskId")] 
        public Task Task { get; set; } = null!;

        [ForeignKey("StatusId")] 
        public Status Status { get; set; } = null!;

        [ForeignKey("AuthorId")] 
        public BoardUser Author { get; set; } = null!;
    }
}