namespace KanbanBoard.Models.Responses
{
    public class TaskResponse
    {
        public int TaskId { get; set; }
        public int BoardId { get; set; }
        public string TaskName { get; set; } = null!;
        public string? TaskDescription { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime DateOfMade { get; set; }
        public int Order { get; set; }
        public StatusResponse Status { get; set; } = null!;
        public UserResponse? Worker { get; set; }
        public UserResponse Author { get; set; } = null!;
        public int CommentsCount { get; set; }
        public int AttachmentsCount { get; set; }
    }
}
