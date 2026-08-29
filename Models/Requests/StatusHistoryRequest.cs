using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests
{
    public class StatusHistoryRequest
    {
        [Range(1, int.MaxValue)]
        public int NewStatusId { get; set; }
    }
}
