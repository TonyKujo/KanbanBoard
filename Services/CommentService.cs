using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Requests;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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

        public async Task<bool> DeleteCommentFromTaskAsync(int boardId, int userId, int taskId, int commentId, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
            {
                return false;
            }

            var authorFromThisBoard = await _db.BoardUsers.FirstOrDefaultAsync(bu => bu.UserId == userId && bu.BoardId == boardId, ct);

            if (authorFromThisBoard == null)
                return false;

            var commentToDelete = await _db.Comments
            .Include(c => c.Task)
            .FirstOrDefaultAsync(c => c.CommentId == commentId
                                     && c.TaskId == taskId
                                     && c.Task.BoardId == boardId, ct);

            if (commentToDelete == null)
                return false;

            if (commentToDelete.AuthorId != authorFromThisBoard.BoardUserId)
                return false;

            _db.Comments.Remove(commentToDelete);

            await _db.SaveChangesAsync(ct);

            return true;
        }

        public async Task<CommentResponse?> CreateCommentToTaskAsync(int boardId, int userId, int taskId, CommentRequest request, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
            {
                return null;
            }

            var authorFromThisBoard = await _db.BoardUsers.FirstOrDefaultAsync(bu => bu.UserId == userId && bu.BoardId == boardId, ct);

            if (authorFromThisBoard == null)
                return null;

            var taskFromThisBoard = await _db.Tasks.FirstOrDefaultAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);

            if (taskFromThisBoard == null)
                return null;

            var newComment = new Comment
            {
                AuthorId = authorFromThisBoard.BoardUserId,
                TaskId = taskId,
                IsEdited = false,
                Text = request.Text,
                DateOfMade = DateTime.UtcNow,
            };

            _db.Comments.Add(newComment);

            await _db.SaveChangesAsync(ct);

            var createdComment = await _db.Comments
                .Include(c => c.Author).ThenInclude(bu => bu.User)
                .FirstAsync(c => c.CommentId == newComment.CommentId, ct);

            return new CommentResponse
            {
                CommentId = newComment.CommentId,
                Text = newComment.Text,
                MadeDate = newComment.DateOfMade,
                Author = new UserResponse
                {
                    UserId = createdComment.Author.UserId,
                    Login = createdComment.Author.User.Login
                },
            };

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
