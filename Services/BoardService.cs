using KanbanBoard.Data;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;
using KanbanBoard.Models;
namespace KanbanBoard.Services
{
    public class BoardService
    {
        private readonly KanbanBoardDbContext _db;

        public BoardService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }

        public async Task<AllUserBoardsResponse> CreateNewBoardAsync (int userId, string name, string description, CancellationToken ct)
        {
            var board = new Board
            {
                NameOfBoard = name,
                Description = description,
                AuthorId = userId,
                DateOfMade = DateTime.UtcNow
            };
            var boardUser = new BoardUser
            {
                UserId = userId,
                Board = board,
                DateOfJoin = DateTime.UtcNow
            };
            _db.Boards.Add(board);
            _db.BoardUsers.Add(boardUser);


            await _db.SaveChangesAsync(ct);


            return new AllUserBoardsResponse
            {
                BoardId = board.BoardId,
                NameOfBoard = board.NameOfBoard,
                Description = board.Description,
                AuthorId = board.AuthorId,
                DateOfMade = board.DateOfMade
            };

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
