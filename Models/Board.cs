using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models
{
    public class Board
    {
        public Board() { DateOfMade = DateTime.UtcNow; }

        [Key] public int BoardId { get; set; }
        public string NameOfBoard { get; set; }
        public string Description { get; set; }
        public int AuthorId { get; set; }
        public DateTime DateOfMade { get; set; }

        public User Author { get; set; }
        public ICollection<Task> Tasks { get; set; } = new List<Task>();
        public ICollection<BoardUser> BoardUsers { get; set; } = new List<BoardUser>();
        public ICollection<Status> Statuses { get; set; } = new List<Status>();
    }
}
