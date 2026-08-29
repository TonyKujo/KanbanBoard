namespace KanbanBoard.Models.Responses
{
    public class CommentResponse
    {
        public int CommentId { get; set; }
        public string Text { get; set; } = null!;
        public DateTime MadeDate { get; set; }
        public bool IsEdited { get; set; }
        public UserResponse Author { get; set; } = null!;
    }
}