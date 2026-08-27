using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class BoardUser
    {
        [Key] 
        public int BoardUserId { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        [ForeignKey("Board")]
        public int BoardId { get; set; }
        public DateTime DateOfJoin { get; set; }

        public User User { get; set; } = null!;
        public Board Board { get; set; } = null!;

        [InverseProperty("Author")]
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        [InverseProperty("Author")]
        public ICollection<Task> AuthoredTasks { get; set; } = new List<Task>();
        [InverseProperty("Assignee")]
        public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();

        [InverseProperty("Uploader")]
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}
