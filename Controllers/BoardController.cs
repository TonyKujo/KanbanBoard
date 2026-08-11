using KanbanBoard.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KanbanBoard.Controllers
{
    public class BoardController(BoardService boardService) : Controller
    {
        private readonly BoardService _boardService = boardService;
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route ("api/boards")]
        public async Task<IActionResult> GetAllUserBoards(CancellationToken ct)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized("Пользователь не авторизован");

            var result = await _boardService.GetAllUserBoardsAsync(userId, ct);
            return Ok(result);
        }
    }
}
