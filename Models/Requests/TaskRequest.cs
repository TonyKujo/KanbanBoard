namespace KanbanBoard.Models.Requests
{
    public class TaskRequest
    {
        public string TaskName { get; set; } = null!;
        public string? TaskDescription { get; set; }
        public DateTime Deadline { get; set; }
        public int WorkerId { get; set; }
    }
}
