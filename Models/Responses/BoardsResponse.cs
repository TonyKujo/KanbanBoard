namespace KanbanBoard.Models.Responses
{
    public class BoardsResponse
    {
        public int BoardId { get; set; }
        public string NameOfBoard { get; set; } = null!;
        public string? Description { get; set; }
        public AuthorResponse Author { get; set; } = null!;
        public DateTime DateOfMade { get; set; }
    }
}
