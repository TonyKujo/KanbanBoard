using KanbanBoard.Models.Requests;
using KanbanBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace KanbanBoard.Controllers
{
    [Authorize]
    public class CommentController(CommentService commentService) : Controller
    {
        private readonly CommentService _commentService = commentService;

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim!);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}/comments")]
        public async Task<IActionResult> GetTaskComments(int boardId, int taskId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _commentService.GetAllCommentsOfTaskAsync(boardId, userId, taskId, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}
