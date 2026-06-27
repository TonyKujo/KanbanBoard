using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models
{
    public class User
    {
        public User() { DateOfRegistration = DateTime.UtcNow; }

        [Key] public int UserId { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public DateTime DateOfRegistration { get; set; }

        public ICollection<BoardUser> BoardUsers { get; set; } = new List<BoardUser>();
    }
}
