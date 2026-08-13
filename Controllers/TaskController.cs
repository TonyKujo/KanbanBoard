using KanbanBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanbanBoard.Controllers
{
    [Authorize]
    public class TaskController(TaskService taskService) : Controller
    {
        private readonly TaskService _taskService = taskService;

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route ("api/boards/{boardId}/tasks")]

        public async Task<IActionResult> GetAllBoardTasksAsync(int boardId, CancellationToken ct)
        {
            var result = await _taskService.GetAllBoardTasksAsync(boardId, ct);

            return Ok(result);
        }
    }
}
