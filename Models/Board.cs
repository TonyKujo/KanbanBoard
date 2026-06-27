using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models
{
    public class Board
    {
        [Key] 
        public int BoardId { get; set; }
        public string NameOfBoard { get; set; } = null!;
        public string? Description { get; set; }
        public int AuthorId { get; set; }
        public DateTime DateOfMade { get; set; }

        public User Author { get; set; } = null!;
        public ICollection<Task> Tasks { get; set; } = new List<Task>();
        public ICollection<BoardUser> BoardUsers { get; set; } = new List<BoardUser>();
        public ICollection<Status> Statuses { get; set; } = new List<Status>();
    }
}
