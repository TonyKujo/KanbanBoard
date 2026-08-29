using KanbanBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KanbanBoard.Controllers
{
    [ApiController]
    [Authorize]
    public class UserController(UserService userService) : Controller
    {
        private readonly UserService _userService = userService;

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/users/search")]
        public async Task<IActionResult> GetAllUsersForSearch([FromQuery] string query, [FromQuery] int? limit, CancellationToken ct)
        {
            var result = await _userService.GetUsersByLoginAsync(query, limit, ct);
            if(result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
    }
}
