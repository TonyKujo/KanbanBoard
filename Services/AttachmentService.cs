using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;

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
            return await _db.BoardUsers.AnyAsync(bu => bu.BoardId == boardId && bu.UserId == userId, ct);
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
                    FilePath = a.FilePath,
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
                    FilePath = a.FilePath,
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
                .FirstOrDefaultAsync(bu => bu.UserId == userId && bu.BoardId == boardId, ct);
            if (uploader == null)
                return null;

            var task = await _db.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);
            if (task == null || task.AuthorId != uploader.BoardUserId)
                return null;

            return await SaveAttachmentAsync(uploader, taskId, null, file, ct);
        }

        public async Task<AttachmentResponse?> UploadAttachmentToCommentAsync(int boardId, int userId, int commentId, IFormFile file, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return null;

            var uploader = await _db.BoardUsers
                .Include(bu => bu.User)
                .FirstOrDefaultAsync(bu => bu.UserId == userId && bu.BoardId == boardId, ct);
            if (uploader == null)
                return null;

            var comment = await _db.Comments
            .Include(c => c.Task)
            .FirstOrDefaultAsync(c => c.CommentId == commentId && c.Task.BoardId == boardId, ct);
            if (comment == null || comment.AuthorId != uploader.BoardUserId)
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
                FilePath = attachment.FilePath,
                Uploader = new UserResponse
                {
                    UserId = uploader.UserId,
                    Login = uploader.User.Login,
                }
            };
        }
    }
}