using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Requests;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Conventions;
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
            return await _db.BoardUsers.AnyAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);


        }

        private async Task<bool> IsUserBoardOwnerAsync(int boardId, int userId, CancellationToken ct)
        {
            return await _db.Boards.AnyAsync(b => b.BoardId == boardId && b.AuthorId == userId, ct);
        }

        public async Task<CommentResponse?> EditTaskCommentAsync( int boardId, int userId, int taskId, int commentId, CommentRequest request, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return null;

            var authorFromThisBoard = await _db.BoardUsers
                .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);
            if (authorFromThisBoard == null)
                return null;

            var taskFromThisBoard = await _db.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);
            if (taskFromThisBoard == null)
                return null;

            var commentToUpdate = await _db.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId
                                        && c.TaskId == taskId
                                        && c.AuthorId == authorFromThisBoard.BoardUserId, ct);
            if (commentToUpdate == null)
                return null;

            commentToUpdate.Text = request.Text;
            commentToUpdate.IsEdited = true;

            await _db.SaveChangesAsync(ct);

            var updatedComment = await _db.Comments
                .Include(c => c.Author).ThenInclude(bu => bu.User)
                .FirstAsync(c => c.CommentId == commentToUpdate.CommentId, ct);

            return new CommentResponse
            {
                CommentId = updatedComment.CommentId,
                Text = updatedComment.Text,
                MadeDate = updatedComment.DateOfMade,
                Author = new UserResponse
                {
                    UserId = updatedComment.Author.UserId,
                    Login = updatedComment.Author.User.Login
                },
                IsEdited = updatedComment.IsEdited,
            };
        }

        public async Task<bool> DeleteCommentFromTaskAsync(int boardId, int userId, int taskId, int commentId, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return false;

            var authorFromThisBoard = await _db.BoardUsers
                .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);
            if (authorFromThisBoard == null)
                return false;

            var commentToDelete = await _db.Comments
                .Include(c => c.Task)
                .FirstOrDefaultAsync(c => c.CommentId == commentId
                                        && c.TaskId == taskId
                                        && c.Task.BoardId == boardId, ct);
            if (commentToDelete == null)
                return false;

            bool isAuthor = commentToDelete.AuthorId == authorFromThisBoard.BoardUserId;
            bool isOwner = await IsUserBoardOwnerAsync(boardId, userId, ct);

            if (!isAuthor && !isOwner)
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


            var authorFromThisBoard = await _db.BoardUsers.FirstOrDefaultAsync(bu => bu.UserId == userId && bu.BoardId == boardId && !bu.IsDeleted, ct);

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
                IsEdited = createdComment.IsEdited,
            };

        }

        public async Task<List<CommentResponse>?> GetAllCommentsOfTaskAsync(int boardId, int userId, int taskId, CancellationToken ct)
        {
            if(! await IsUserBoardMemberAsync(boardId, userId, ct))
            {
                return null;
            }

            var taskFromThisBoard = await _db.Tasks
                .AnyAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);

            if (!taskFromThisBoard)
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
                    Text = c.Text,
                    IsEdited = c.IsEdited,

                })
                .ToListAsync(ct);

            return comments;
        }
    }
}
