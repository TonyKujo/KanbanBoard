using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class Status
    {
        [Key] 
        public int StatusId { get; set; }
        public string StatusName { get; set; } = null!;
        public int BoardId { get; set; }


        [ForeignKey("BoardId")] 
        public Board Board { get; set; } = null!;
        public ICollection<Task> Tasks { get; set; } = new List<Task>();
        public ICollection<TaskStatusHistory> TaskStatusHistories { get; set; } = new List<TaskStatusHistory>();
    }
}