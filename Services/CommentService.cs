using KanbanBoard.Data;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace KanbanBoard.Services
{
    public class CommentService
    {
        private readonly KanbanBoardDbContext _db;

        public CommentService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }

        private async Task<bool> IsUserBoardMemberAsync(int boardId, int userId, CancellationToken ct)
        {
            return await _db.BoardUsers.AnyAsync(bu => bu.BoardId == boardId && bu.UserId == userId, ct);


        }

        public async Task<List<CommentResponse>?> GetAllCommentsOfTaskAsync(int boardId, int userId, int taskId, CancellationToken ct)
        {
            if(! await IsUserBoardMemberAsync(boardId, userId, ct))
            {
                return null;
            }

            var comments = await _db.Comments
                .Where(c => c.TaskId == taskId)
                .Select(c => new CommentResponse
                {
                    Author = new UserResponse { Login = c.Author.User.Login, UserId = c.Author.UserId },
                    CommentId = c.CommentId,
                    MadeDate = c.DateOfMade,
                    Text = c.Text

                })
                .ToListAsync(ct);

            return comments;
        }
    }
}
