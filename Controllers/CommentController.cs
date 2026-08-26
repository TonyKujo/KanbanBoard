using KanbanBoard.Models;
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

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}/comments")]
        public async Task<IActionResult> CreateTaskComment(int boardId, int taskId, [FromBody] CommentRequest request, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _commentService.CreateCommentToTaskAsync(boardId, userId, taskId, request, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}/comments/{commentId}")]
        public async Task<IActionResult> DeleteTaskComment(int boardId, int taskId, int commentId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _commentService.DeleteCommentFromTaskAsync(boardId, userId, taskId, commentId, ct);

            if (result == false)
                return NotFound();

            return NoContent();
        }
    }
}
