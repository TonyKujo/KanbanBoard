using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class TaskStatusHistory
    {
        [Key] 
        public int StatusChangeId { get; set; }
        [ForeignKey("Task")]
        public int TaskId { get; set; }
        [ForeignKey("Status")]
        public int StatusId { get; set; }
        [ForeignKey("Author")]
        public int AuthorId { get; set; }
        public DateTime ChangeDate { get; set; }

        public Task Task { get; set; } = null!;

        public Status Status { get; set; } = null!;

        public BoardUser Author { get; set; } = null!;
    }
}