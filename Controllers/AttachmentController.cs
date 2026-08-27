using KanbanBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KanbanBoard.Controllers
{
    [Authorize]
    public class AttachmentController(AttachmentService attachmentService) : Controller
    {
        private readonly AttachmentService _attachmentService = attachmentService;

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim!);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}/attachments")]
        public async Task<IActionResult> AddAttachmentToTask(int boardId, int taskId, [FromForm] IFormFile file, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _attachmentService.UploadAttachmentToTaskAsync(boardId, userId, taskId, file, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }


        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/comments/{commentId}/attachments")]
        public async Task<IActionResult> AddAttachmentToComment(int boardId, int commentId, [FromForm] IFormFile file, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _attachmentService.UploadAttachmentToCommentAsync(boardId, userId, commentId, file, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/comments/{commentId}/attachments")]
        public async Task<IActionResult> GetCommentAttachments(int boardId, int commentId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _attachmentService.GetAllCommentsAttachments(boardId, userId, commentId, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}/attachments")]
        public async Task<IActionResult> GetTaskAttachments(int boardId, int taskId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _attachmentService.GetAllTasksAttachments(boardId, userId, taskId, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}
