using KanbanBoard.Models.Requests;
using KanbanBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KanbanBoard.Controllers
{
    [ApiController]
    [Authorize]
    public class BoardController(BoardService boardService) : Controller
    {
        private readonly BoardService _boardService = boardService;

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim!);
        }


        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/users")]
        public async Task<IActionResult> GetBoardUsers(int boardId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _boardService.GetBoardUsersAsync(boardId, userId, ct);

            if(result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route ("api/boards/{boardId}")]
        public async Task<IActionResult> UpdateBoard(int boardId, [FromBody] BoardRequest request, CancellationToken ct)
        {

            int userId = GetUserId();

            var result = await _boardService.UpdateBoardAsync(boardId, userId, request, ct);

            if (result == null)
                return NotFound();
            return Ok(result);
        }


        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("api/boards")]
        public async Task<IActionResult> GetAllUserBoards(CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _boardService.GetAllUserBoardsAsync(userId, ct);
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("api/boards")]
        public async Task<IActionResult> AddNewBoard([FromBody] BoardRequest request, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _boardService.CreateBoardAsync(userId, request, ct);
            return CreatedAtAction(nameof(GetUserBoard), new { boardId = result.BoardId }, result);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}")]
        public async Task<IActionResult> GetUserBoard(int boardId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _boardService.GetBoardAsync(boardId, userId, ct);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/users/{userId}")]
        public async Task<IActionResult> RemoveBoardUser(int boardId, int userId, CancellationToken ct)
        {
            var currentUserId = GetUserId();
            var result = await _boardService.RemoveUserFromBoardAsync(boardId, currentUserId, userId, ct);

            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/users")]
        public async Task<IActionResult> AddBoardUser(int boardId, [FromBody] BoardUserRequest request, CancellationToken ct)
        {
            var userId = GetUserId();
            var result = await _boardService.AddUserToBoardAsync(boardId, userId, request, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("api/boards/{boardId}")]
        public async Task<IActionResult> DeleteUserBoard (int boardId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _boardService.DeleteBoardAsync(boardId, userId, ct);
            if(result == false)
                return NotFound();
            return NoContent();
        }
    }
}
