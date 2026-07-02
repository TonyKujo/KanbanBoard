namespace KanbanBoard.Models.Responses;

public class GetUserInfoRespones
{
    public int UserId { get; set; }
    public string Login { get; set; } = null!;
    public DateTime DateOfRegistration { get; set; }
}
