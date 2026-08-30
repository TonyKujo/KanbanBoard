using KanbanBoard.Models.Requests;
using KanbanBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KanbanBoard.Controllers
{
    [ApiController]
    [Authorize]
    public class TaskController(TaskService taskService) : Controller
    {
        private readonly TaskService _taskService = taskService;

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim!);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks")]
        public async Task<IActionResult> GetAllBoardTasksAsync(int boardId,  CancellationToken ct,  [FromQuery] int? statusId = null,  [FromQuery] string? search = null)
        {
            var userId = GetUserId();
            var result = await _taskService.GetAllBoardTasksAsync(boardId, userId, ct, statusId, search);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}")]
        public async Task<IActionResult> GetBoardTask(int boardId, int taskId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _taskService.GetBoardTaskAsync(boardId, userId, taskId, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}/history")]
        public async Task<IActionResult> GetTaskHistory(int boardId, int taskId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _taskService.GetTaskHistoryAsync(boardId, userId, taskId, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks")]
        public async Task<IActionResult> AddNewTask(int boardId, [FromBody] TaskRequest request, CancellationToken ct)
        {
            int userId = GetUserId();

            var result = await _taskService.CreateTaskAsync(boardId, userId, request, ct);

            if(result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}")]
        public async Task<IActionResult> UpdateTask(int boardId,int taskId, [FromBody] TaskRequest request, CancellationToken ct)
        {
            int userId = GetUserId();

            var result = await _taskService.UpdateTaskAsync(boardId, userId, taskId, request, ct);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}")]
        public async Task<IActionResult> DeleteTask(int boardId, int taskId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _taskService.DeleteTaskAsync(boardId, userId, taskId, ct);
            if (result == false)
                return NotFound();
            return NoContent();
        }

        [HttpPatch]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}/position")]
        public async Task<IActionResult> MoveTask(int boardId, int taskId, [FromBody] TaskPositionRequest request, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _taskService.MoveTaskAsync(boardId, userId, taskId, request, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPatch]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}/status")]
        public async Task<IActionResult> ChangeStatus(int boardId, int taskId, [FromBody] StatusHistoryRequest request, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _taskService.ChangeTaskStatusAsync(boardId, userId, taskId, request, ct);
            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}
