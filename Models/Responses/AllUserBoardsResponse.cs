namespace KanbanBoard.Models.Responses
{
    public class AllUserBoardsResponse
    {
        public int BoardId { get; set; }
        public string NameOfBoard { get; set; } = null!;
        public string? Description { get; set; }
        public int AuthorId { get; set; }
        public DateTime DateOfMade { get; set; }
    }
}
