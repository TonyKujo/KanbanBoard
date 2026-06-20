using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models
{
    public class BoardUser
    {
        public BoardUser() { DateOfJoin = DateTime.UtcNow; }

        [Key] public int BoardUserId { get; set; }
        public int UserId { get; set; }
        public int BoardId { get; set; }
        public DateTime DateOfJoin { get; set; }

        public User User { get; set; }
        public Board Board { get; set; }
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Task> AuthoredTasks { get; set; } = new List<Task>();
        public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();
    }
}
