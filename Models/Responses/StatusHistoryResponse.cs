namespace KanbanBoard.Models.Responses
{
    public class StatusHistoryResponse
    {
        public int TaskId { get; set; }
        public StatusResponse Status { get; set; }
        public DateTime LastStatusChangeDate { get; set; }
    }
}
