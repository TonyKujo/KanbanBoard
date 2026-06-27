using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class User
    {
        [Key] 
        public int UserId { get; set; }

        [MaxLength(100)]
        public string Login { get; set; } = null!;
        [MaxLength(1000)]
        public string PasswordHash { get; set; } = null!;
        public DateTime DateOfRegistration { get; set; }

        [InverseProperty("User")]
        public ICollection<BoardUser> BoardUsers { get; set; } = new List<BoardUser>();
    }
}
