namespace KanbanBoard.Models.Responses
{
    public class StatusResponse
    {
        public int StatusId {  get; set; }
        public string StatusName { get; set; } = null!;
        public int Order { get; set; }
    }
}
