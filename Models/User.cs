using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class User
    {
        [Key] 
        public int UserId { get; set; }
        public string Login { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public DateTime DateOfRegistration { get; set; }

        [InverseProperty("User")]
        public ICollection<BoardUser> BoardUsers { get; set; } = new List<BoardUser>();
    }
}
