namespace KanbanBoard.Models.Responses
{
    public class TaskResponse
    {
        public int TaskId { get; set; }
        public string TaskName { get; set; } = null!;
        public string? TaskDescription { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime DateOfMade { get; set; }
        public StatusResponse Status { get; set; } = null!;
        public UserResponse Worker { get; set; } = null!;
        public UserResponse Author { get; set; } = null!;
    }
}
