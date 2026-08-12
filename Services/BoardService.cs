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

        public async Task<bool> DeleteBoardAsync(int boardId, int userId, CancellationToken ct)
        {
            var board = await _db.Boards
                .FirstOrDefaultAsync(b => b.BoardId == boardId && b.BoardUsers.Any(bu => bu.UserId == userId), ct);
            if (board is null)
                return false;

            _db.Boards.Remove(board);

            await _db.SaveChangesAsync(ct);

            return true;
        }

        public async Task<BoardsResponse?> GetBoardAsync(int boardId, int userId, CancellationToken ct)
        {
            var board = await _db.Boards
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.BoardId == boardId && b.BoardUsers.Any(bu => bu.UserId == userId), ct);

            if (board is null)
                return null;

            return new BoardsResponse
            {
                BoardId = board.BoardId,
                NameOfBoard = board.NameOfBoard,
                Description = board.Description,
                Author = new AuthorResponse
                {
                    AuthorId = board.Author.UserId,
                    Login = board.Author.Login
                },
                DateOfMade = board.DateOfMade
            };
        }
        public async Task<BoardsResponse> CreateNewBoardAsync (int userId, string name, string description, CancellationToken ct)
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


            return new BoardsResponse
            {
                BoardId = board.BoardId,
                NameOfBoard = board.NameOfBoard,
                Description = board.Description,
                Author = new AuthorResponse { AuthorId = board.AuthorId },
                DateOfMade = board.DateOfMade
            };

        }
        public async Task<List<BoardsResponse>> GetAllUserBoardsAsync( int userId, CancellationToken ct) 
        {
            var boards = await _db.Boards
            .Where(b => b.BoardUsers.Any(bu => bu.UserId == userId))
            .Select(b => new BoardsResponse
            {
                BoardId = b.BoardId,
                NameOfBoard = b.NameOfBoard,
                Description = b.Description,
                Author = new AuthorResponse { AuthorId = b.AuthorId },
                DateOfMade = b.DateOfMade
            })
            .ToListAsync(ct);

            return boards;
        }

    }
}
