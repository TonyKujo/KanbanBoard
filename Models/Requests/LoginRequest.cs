namespace KanbanBoard.Models.Requests;

public class LoginRequest
{
    public string Login { get; set; } = null!;
    public string Password { get; set; } = null!;
}
