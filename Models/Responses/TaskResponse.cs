namespace KanbanBoard.Models.Responses
{
    public class TaskResponse
    {
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public string TaskDescription { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime DateOfMade { get; set; }
        public StatusResponse Status { get; set; }
        public UserResponse Worker {  get; set; }
        public UserResponse Author { get; set; }
    }
}
