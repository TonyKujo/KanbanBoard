using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class Status
    {
        [Key] 
        public int StatusId { get; set; }
        [MaxLength(10)]
        public string StatusName { get; set; } = null!;
        [ForeignKey("Board")]
        public int BoardId { get; set; }


        public Board Board { get; set; } = null!;
        [InverseProperty("Status")]
        public ICollection<Task> Tasks { get; set; } = new List<Task>();
        [InverseProperty("Status")]
        public ICollection<TaskStatusHistory> TaskStatusHistories { get; set; } = new List<TaskStatusHistory>();
    }
}