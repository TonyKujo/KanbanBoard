using KanbanBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KanbanBoard.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/boards/{boardId}")]
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
        [Route("tasks/{taskId}/attachments")]
        public async Task<IActionResult> AddAttachmentToTask(int boardId, int taskId, IFormFile file, CancellationToken ct)
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
        [Route("comments/{commentId}/attachments")]
        public async Task<IActionResult> AddAttachmentToComment(int boardId, int commentId, IFormFile file, CancellationToken ct)
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
        [Route("comments/{commentId}/attachments")]
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
        [Route("tasks/{taskId}/attachments")]
        public async Task<IActionResult> GetTaskAttachments(int boardId, int taskId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _attachmentService.GetAllTasksAttachments(boardId, userId, taskId, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("attachments/{attachmentId}/download")]
        public async Task<IActionResult> DownloadAttachment(int boardId, int attachmentId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _attachmentService.DownloadAttachmentAsync(boardId, userId, attachmentId, ct);
            if (result == null)
                return NotFound();

            var (stream, fileName, contentType) = result.Value;
            return File(stream, contentType, fileName);
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("attachments/{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(int boardId, int attachmentId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _attachmentService.DeleteAttachmentAsync(boardId, userId, attachmentId, ct);

            if (result == false)
                return NotFound();
            return NoContent();
        }
    }
}
