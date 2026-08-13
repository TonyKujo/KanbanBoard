using KanbanBoard.Data;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;
using KanbanBoard.Models;
using KanbanBoard.Models.Requests;
namespace KanbanBoard.Services
{
    public class BoardService
    {
        private readonly KanbanBoardDbContext _db;

        public BoardService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }


        public async Task<BoardResponse?> UpdateBoardAsync(int boardId, int userId, BoardRequest boardRequest, CancellationToken ct)
        {
            var board = await _db.Boards
                .FirstOrDefaultAsync(b => b.BoardId == boardId && b.AuthorId == userId && b.BoardUsers.Any(bu => bu.UserId == userId), ct);

            if (board is null)
                return null;

            board.NameOfBoard = boardRequest.Name;
            board.Description = boardRequest.Description;

            await _db.SaveChangesAsync(ct);

            return new BoardResponse
            {
                BoardId = board.BoardId,
                NameOfBoard = board.NameOfBoard,
                Description = board.Description,
                Author = new UserResponse { UserId = board.AuthorId },
                DateOfMade = board.DateOfMade
            };
        }


        public async Task<bool> DeleteBoardAsync(int boardId, int userId, CancellationToken ct)
        {
            var board = await _db.Boards
                .FirstOrDefaultAsync(b => b.BoardId == boardId && b.AuthorId == userId && b.BoardUsers.Any(bu => bu.UserId == userId), ct);
            if (board is null)
                return false;

            _db.Boards.Remove(board);

            await _db.SaveChangesAsync(ct);

            return true;
        }

        public async Task<BoardResponse?> GetBoardAsync(int boardId, int userId, CancellationToken ct)
        {
            var board = await _db.Boards
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.BoardId == boardId && b.BoardUsers.Any(bu => bu.UserId == userId), ct);

            if (board is null)
                return null;

            return new BoardResponse
            {
                BoardId = board.BoardId,
                NameOfBoard = board.NameOfBoard,
                Description = board.Description,
                Author = new UserResponse
                {
                    UserId = board.Author.UserId,
                    Login = board.Author.Login
                },
                DateOfMade = board.DateOfMade
            };
        }
        public async Task<BoardResponse> CreateBoardAsync (int userId, BoardRequest request, CancellationToken ct)
        {
            var board = new Board
            {
                NameOfBoard = request.Name,
                Description = request.Description,
                AuthorId = userId,
                DateOfMade = DateTime.UtcNow
            };
            var boardUser = new BoardUser
            {
                UserId = userId,
                Board = board,
                DateOfJoin = DateTime.UtcNow
            };
            _db.BoardUsers.Add(boardUser);


            await _db.SaveChangesAsync(ct);


            return new BoardResponse
            {
                BoardId = board.BoardId,
                NameOfBoard = board.NameOfBoard,
                Description = board.Description,
                Author = new UserResponse { UserId = board.AuthorId },
                DateOfMade = board.DateOfMade
            };

        }
        public async Task<List<BoardResponse>> GetAllUserBoardsAsync( int userId, CancellationToken ct) 
        {
            var boards = await _db.Boards
            .Where(b => b.BoardUsers.Any(bu => bu.UserId == userId))
            .Select(b => new BoardResponse
            {
                BoardId = b.BoardId,
                NameOfBoard = b.NameOfBoard,
                Description = b.Description,
                Author = new UserResponse { UserId = b.AuthorId },
                DateOfMade = b.DateOfMade
            })
            .ToListAsync(ct);

            return boards;
        }

    }
}
