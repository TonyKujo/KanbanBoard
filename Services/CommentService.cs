using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Requests;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Conventions;
using System.Linq.Expressions;
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

        private static readonly Expression<Func<Comment, CommentResponse>> ToCommentResponse = c => new CommentResponse
        {
            CommentId = c.CommentId,
            TaskId = c.TaskId,
            Text = c.Text,
            MadeDate = c.DateOfMade,
            IsEdited = c.IsEdited,
            Author = new UserResponse
            {
                UserId = c.Author.UserId,
                Login = c.Author.User.Login
            },
            Attachments = c.Attachments.Select(a => new AttachmentResponse
            {
                AttachmentId = a.AttachmentId,
                FileName = a.FileName,
                DateOfUpload = a.DateOfUpload,
                Uploader = new UserResponse
                {
                    UserId = a.Uploader.UserId,
                    Login = a.Uploader.User.Login
                }
            }).ToList()
        };

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

            return await _db.Comments
                .Where(c => c.CommentId == commentToUpdate.CommentId)
                .Select(ToCommentResponse)
                .FirstAsync(ct);
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

            return await _db.Comments
                .Where(c => c.CommentId == newComment.CommentId)
                .Select(ToCommentResponse)
                .FirstAsync(ct);

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
                .OrderBy(c => c.DateOfMade)
                .ThenBy(c => c.CommentId)
                .Select(ToCommentResponse)
                .ToListAsync(ct);

            return comments;
        }
    }
}
