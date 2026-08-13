namespace KanbanBoard.Models.Responses
{
    public class TaskResponse
    {
        public int TaskId { get; set; }
        public string NameOfTask { get; set; }
        public string DescriptionOfTask { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime DateOfMade { get; set; }
        public StatusResponse Status { get; set; }
        public UserResponse Worker {  get; set; }
    }
}
