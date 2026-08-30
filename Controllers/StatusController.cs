using KanbanBoard.Models.Requests;
using KanbanBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KanbanBoard.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/boards/{boardId:int}/statuses")]
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
        [Route("")]
        public async Task<IActionResult> GetBoardStatuses(int boardId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _statusService.GetBoardStatusesAsync(boardId, userId, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("")]
        public async Task<IActionResult> CreateBoardStatus(int boardId, [FromBody] StatusRequest request, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _statusService.CreateStatusAsync(boardId, userId, request, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{statusId}")]
        public async Task<IActionResult> UpdateBoardStatus(int boardId, int statusId, [FromBody] StatusRequest request, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _statusService.UpdateStatusAsync(boardId, userId, statusId, request, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPatch]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("{statusId:int}/position")]
        public async Task<IActionResult> MoveBoardStatus(int boardId, int statusId, [FromBody] StatusPositionRequest request, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _statusService.MoveStatusAsync(boardId, userId, statusId, request, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [Route("{statusId:int}")]
        public async Task<IActionResult> DeleteBoardStatus(int boardId, int statusId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _statusService.DeleteStatusAsync(boardId, userId, statusId, ct);

            if (result == DeleteStatusResult.NotFound)
                return NotFound();

            if (result == DeleteStatusResult.HasTasks)
                return Conflict("В колонке есть задачи");

            if (result == DeleteStatusResult.LastStatus)
                return Conflict("Нельзя удалить последнюю колонку");

            return NoContent();
        }
    }
}
