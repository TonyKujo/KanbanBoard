using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class Board
    {
        [Key] 
        public int BoardId { get; set; }
        [MaxLength(200)]
        public string NameOfBoard { get; set; } = null!;
        [MaxLength(3000)]
        public string? Description { get; set; }
        public int AuthorId { get; set; }
        public DateTime DateOfMade { get; set; }

        public User Author { get; set; } = null!;

        [InverseProperty("Board")]
        public ICollection<Task> Tasks { get; set; } = new List<Task>();

        [InverseProperty("Board")]
        public ICollection<BoardUser> BoardUsers { get; set; } = new List<BoardUser>();

        [InverseProperty("Board")]
        public ICollection<Status> Statuses { get; set; } = new List<Status>();
    }
}
