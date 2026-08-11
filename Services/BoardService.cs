using KanbanBoard.Data;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace KanbanBoard.Services
{
    public class BoardService
    {
        private readonly KanbanBoardDbContext _db;

        public BoardService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }


        public async Task<List<AllUserBoardsResponse>> GetAllUserBoardsAsync( int userId, CancellationToken ct) 
        {
            var boards = await _db.Boards
            .Where(b => b.BoardUsers.Any(bu => bu.UserId == userId))
            .Select(b => new AllUserBoardsResponse
            {
                BoardId = b.BoardId,
                NameOfBoard = b.NameOfBoard,
                Description = b.Description,
                AuthorId = b.AuthorId,
                DateOfMade = b.DateOfMade
            })
            .ToListAsync(ct);

            return boards;
        }

    }
}
