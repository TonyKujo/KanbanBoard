using KanbanBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KanbanBoard.Controllers
{
    [Authorize]
    public class StatusController(StatusService statusService) : Controller
    {
        private readonly StatusService _statusService = statusService;

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim!);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/statuses")]
        public async Task<IActionResult> GetBoardStatuses(int boardId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _statusService.GetBoardStatusesAsync(boardId, userId, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}