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
        private readonly StatusService _statusService;

        public BoardService(KanbanBoardDbContext dbContext, StatusService statusService)
        {
            _db = dbContext;
            _statusService = statusService;
        }

        private async Task<bool> IsUserBoardMemberAsync(int boardId, int userId, CancellationToken ct)
        {
            return await _db.BoardUsers.AnyAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);
        }

        public async Task<UserResponse?> AddUserToBoardAsync(int boardId, int currentUserId, BoardUserRequest request, CancellationToken ct)
        {
            var board = await _db.Boards
                .FirstOrDefaultAsync(b => b.BoardId == boardId && b.AuthorId == currentUserId, ct);
            if (board == null)
                return null;

            var userToAdd = await _db.Users
                .FirstOrDefaultAsync(u => u.Login == request.Login, ct);
            if (userToAdd == null)
                return null;

            var existingBoardUser = await _db.BoardUsers
                .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userToAdd.UserId, ct);

            if (existingBoardUser != null)
            {
                existingBoardUser.IsDeleted = false;
                existingBoardUser.DateOfJoin = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                return new UserResponse { UserId = userToAdd.UserId, Login = userToAdd.Login };
            }

            var boardUser = new BoardUser
            {
                BoardId = boardId,
                UserId = userToAdd.UserId,
                DateOfJoin = DateTime.UtcNow
            };
            _db.BoardUsers.Add(boardUser);
            await _db.SaveChangesAsync(ct);
            return new UserResponse { UserId = userToAdd.UserId, Login = userToAdd.Login };
        }

        public async Task<bool> RemoveUserFromBoardAsync(int boardId, int currentUserId, int userIdToRemove, CancellationToken ct)
        {
            var board = await _db.Boards
                .FirstOrDefaultAsync(b => b.BoardId == boardId && b.AuthorId == currentUserId, ct);
            if (board == null)
                return false;

            if (userIdToRemove == currentUserId)
                return false;

            var userToRemove = await _db.BoardUsers
                .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userIdToRemove, ct);
            if (userToRemove == null)
                return false;

            userToRemove.IsDeleted = true;
            userToRemove.DateOfJoin = DateTime.UtcNow;

            var assignedTasks = await _db.Tasks
                .Where(t => t.BoardId == boardId && t.AssigneeId == userToRemove.BoardUserId)
                .ToListAsync(ct);
            foreach (var task in assignedTasks)
                task.AssigneeId = null;

            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<List<UserResponse>?> GetBoardUsersAsync(int boardId, int userId, CancellationToken ct)
        {
            if(! await IsUserBoardMemberAsync(boardId, userId, ct))
                return null;

            var users = await _db.BoardUsers
            .Where(bu => bu.BoardId == boardId && !bu.IsDeleted)
            .Select(bu => new UserResponse
            {
                Login = bu.User.Login,
                UserId = bu.UserId,
            })
            .ToListAsync(ct);

            return users;
        }

        public async Task<BoardResponse?> UpdateBoardAsync(int boardId, int userId, BoardRequest boardRequest, CancellationToken ct)
        {
            var board = await _db.Boards
                .FirstOrDefaultAsync(b => b.BoardId == boardId && b.AuthorId == userId
                                        && b.BoardUsers.Any(bu => bu.UserId == userId && !bu.IsDeleted), ct);


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
                .FirstOrDefaultAsync(b => b.BoardId == boardId && b.AuthorId == userId
                                        && b.BoardUsers.Any(bu => bu.UserId == userId && !bu.IsDeleted), ct);

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
                .FirstOrDefaultAsync(b => b.BoardId == boardId && b.BoardUsers.Any(bu => bu.UserId == userId && !bu.IsDeleted), ct);

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

            await _statusService.CreateDefaultStatusesAsync(board.BoardId, ct);


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
            .Where(b => b.BoardUsers.Any(bu => bu.UserId == userId && !bu.IsDeleted))
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
