namespace KanbanBoard.Models.Responses
{
    public class CommentResponse
    {
        public int CommentId { get; set; }
        public string? Text { get; set; }
        public DateTime MadeDate { get; set; }
        public UserResponse Author { get; set; } = null!;
    }
}
