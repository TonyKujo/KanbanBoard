namespace KanbanBoard.Models.Responses
{
    public class CommentResponse
    {
        public int CommentId { get; set; }
        public int TaskId { get; set; }
        public string? Text { get; set; }
        public DateTime MadeDate { get; set; }
        public bool IsEdited { get; set; }
        public UserResponse Author { get; set; } = null!;
        public List<AttachmentResponse> Attachments { get; set; } = new List<AttachmentResponse>();
    }
}
