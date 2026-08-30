namespace KanbanBoard.Models.Responses
{
    public class StatusHistoryResponse
    {
        public int StatusChangeId { get; set; }
        public int TaskId { get; set; }
        public StatusResponse Status { get; set; } = null!;
        public DateTime LastStatusChangeDate { get; set; }
        public UserResponse Author { get; set; } = null!;
    }
}
