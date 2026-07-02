using System.Security.Claims;

namespace KanbanBoard.Models.Dto
{
    public class AuthDto
    {
        public ClaimsPrincipal ClaimsPrincipal { get; set; } = null!;
    }
}
