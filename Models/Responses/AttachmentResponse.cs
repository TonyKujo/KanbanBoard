namespace KanbanBoard.Models.Responses
{
    public class AttachmentResponse
    {
        public int AttachmentId { get; set; }
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public DateTime DateOfUpload { get; set; }
        public UserResponse Uploader { get; set; } = null!;
    }
}