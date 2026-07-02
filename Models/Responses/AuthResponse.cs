using System.Security.Claims;

namespace KanbanBoard.Models.Responses;

public class AuthResponse
{
    public ClaimsPrincipal ClaimsPrincipal { get; set; } = null!;
}
