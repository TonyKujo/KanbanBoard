using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Threading.Tasks;
using Attachment = KanbanBoard.Models.Attachment;

namespace KanbanBoard.Services
{
    public class AttachmentService
    {
        private readonly KanbanBoardDbContext _db;

        public AttachmentService(KanbanBoardDbContext dbContext)
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

        public async Task<bool> DeleteAttachmentAsync(int boardId, int userId, int attachmentId, CancellationToken ct)
        {
            bool isOwner = await IsUserBoardOwnerAsync(boardId, userId, ct);

            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return false;

            var AttachmentToDelete = await _db.Attachments
                .Include(a => a.Task)
                .Include(a => a.Comment).ThenInclude(c => c.Task)
                .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId, ct);

            if (AttachmentToDelete == null)
                return false;

            if ((AttachmentToDelete.Task != null && AttachmentToDelete.Task.BoardId != boardId))
                return false;
            if (AttachmentToDelete.Comment != null && AttachmentToDelete.Comment.Task.BoardId != boardId)
                return false;

            var uploader = await _db.BoardUsers
                .Include(bu => bu.User)
                .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);

            if (uploader == null)
                return false;



            bool isTaskAuthor = AttachmentToDelete.Task != null && AttachmentToDelete.Task.AuthorId == uploader.BoardUserId;
            bool isCommentAuthor = AttachmentToDelete.Comment != null && AttachmentToDelete.Comment.AuthorId == uploader.BoardUserId;

            if (!isOwner && !isTaskAuthor && !isCommentAuthor)
                return false;


            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), AttachmentToDelete.FilePath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            _db.Attachments.Remove(AttachmentToDelete);
            await _db.SaveChangesAsync(ct);

            return true;
        }

        public async Task<(Stream Stream, string FileName, string ContentType)?> DownloadAttachmentAsync(int boardId, int userId, int attachmentId, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return null;


            var attachment = await _db.Attachments
                .Include(a => a.Task)
                .Include(a => a.Comment).ThenInclude(c => c.Task)
                .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId, ct);

            if (attachment == null)
                return null;

            if (attachment.Task != null && attachment.Task.BoardId != boardId)
                return null;
            if (attachment.Comment != null && attachment.Comment.Task.BoardId != boardId)
                return null;

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), attachment.FilePath);
            if (!File.Exists(fullPath))
                return null;

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

            var contentType = "application/octet-stream";


            return (stream, attachment.FileName, contentType);
        }

        public async Task<List<AttachmentResponse>?> GetAllCommentsAttachments(int boardId, int userId, int commentId, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return null;

            var attachments = await _db.Attachments
                .Where(a => a.Comment != null && a.CommentId == commentId && a.Comment!.Task.BoardId == boardId)
                .Select(a => new AttachmentResponse
                {
                    AttachmentId = a.AttachmentId,
                    DateOfUpload = a.DateOfUpload,
                    FileName = a.FileName,
                    Uploader = new UserResponse
                    {
                        Login = a.Uploader.User.Login,
                        UserId = a.Uploader.UserId
                    },
                })
                .ToListAsync(ct);

            return attachments;
        }

        public async Task<List<AttachmentResponse>?> GetAllTasksAttachments(int boardId, int userId, int taskId, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return null;

            var attachments = await _db.Attachments
                .Where(a => a.Task != null && a.TaskId == taskId && a.Task!.BoardId == boardId)
                .Select(a => new AttachmentResponse 
                {
                    AttachmentId = a.AttachmentId,
                    DateOfUpload = a.DateOfUpload,
                    FileName = a.FileName,
                    Uploader = new UserResponse
                    {
                        Login = a.Uploader.User.Login,
                        UserId = a.Uploader.UserId
                    },
                })
                .ToListAsync(ct);

            return attachments;
        }

        public async Task<AttachmentResponse?> UploadAttachmentToTaskAsync(int boardId, int userId, int taskId, IFormFile file, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return null;

            var uploader = await _db.BoardUsers
                .Include(bu => bu.User)
                .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);
            if (uploader == null)
                return null;

            var task = await _db.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);

            if (task == null)
                return null;

            bool isOwner = await IsUserBoardOwnerAsync(boardId, userId, ct);

            if (!isOwner && task.AuthorId != uploader.BoardUserId)
                return null;


            return await SaveAttachmentAsync(uploader, taskId, null, file, ct);
        }

        public async Task<AttachmentResponse?> UploadAttachmentToCommentAsync(int boardId, int userId, int commentId, IFormFile file, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return null;

            var uploader = await _db.BoardUsers
                .Include(bu => bu.User)
                .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);
            if (uploader == null)
                return null;

            var comment = await _db.Comments
            .Include(c => c.Task)
            .FirstOrDefaultAsync(c => c.CommentId == commentId && c.Task.BoardId == boardId, ct);

            if (comment == null)
                return null;

            bool isOwner = await IsUserBoardOwnerAsync(boardId, userId, ct);

            if (!isOwner && comment.AuthorId != uploader.BoardUserId)
                return null;


            return await SaveAttachmentAsync(uploader, null, commentId, file, ct);
        }

        private async Task<AttachmentResponse?> SaveAttachmentAsync(BoardUser uploader, int? taskId, int? commentId, IFormFile file, CancellationToken ct)
        {
            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Attachments");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(uploadFolder, uniqueFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            var attachment = new Attachment
            {
                DateOfUpload = DateTime.UtcNow,
                FileName = Path.GetFileName(file.FileName),
                FilePath = Path.Combine("Storage", "Attachments", uniqueFileName),
                TaskId = taskId,
                CommentId = commentId,
                UploaderId = uploader.BoardUserId,
            };

            _db.Attachments.Add(attachment);
            await _db.SaveChangesAsync(ct);

            return new AttachmentResponse
            {
                AttachmentId = attachment.AttachmentId,
                DateOfUpload = attachment.DateOfUpload,
                FileName = attachment.FileName,
                Uploader = new UserResponse
                {
                    UserId = uploader.UserId,
                    Login = uploader.User.Login,
                }
            };
        }
    }
}