using System.Linq.Expressions;
using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Requests;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Task = KanbanBoard.Models.Task;
namespace KanbanBoard.Services
{
    public class TaskService
    {
        private readonly KanbanBoardDbContext _db;

        public TaskService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private static readonly Expression<Func<Task, TaskResponse>> ToTaskResponse = t => new TaskResponse
        {
            TaskId = t.TaskId,
            BoardId = t.BoardId,
            TaskName = t.TaskName,
            TaskDescription = t.TaskDescription,
            Deadline = t.DeadLine,
            DateOfMade = t.CreationDate,
            Order = t.Order,
            Status = new StatusResponse
            {
                StatusId = t.Status.StatusId,
                StatusName = t.Status.StatusName,
                Order = t.Status.Order
            },
            Worker = t.Assignee == null ? null : new UserResponse
            {
                UserId = t.Assignee.UserId,
                Login = t.Assignee.User.Login
            },
            Author = new UserResponse
            {
                UserId = t.Author.UserId,
                Login = t.Author.User.Login
            },
            CommentsCount = t.Comments.Count,
            AttachmentsCount = t.Attachments.Count
        };

        private async Task<TaskResponse?> GetTaskResponseAsync(int boardId, int taskId, CancellationToken ct)
        {
            return await _db.Tasks
                .Where(t => t.BoardId == boardId && t.TaskId == taskId)
                .Select(ToTaskResponse)
                .FirstOrDefaultAsync(ct);
        }

        private async Task<bool> IsUserBoardMemberAsync(int boardId, int userId, CancellationToken ct)
        {
            return await _db.BoardUsers.AnyAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);


        }

        private async Task<bool> IsUserBoardOwnerAsync(int boardId, int userId, CancellationToken ct)
        {
            return await _db.Boards.AnyAsync(b => b.BoardId == boardId && b.AuthorId == userId, ct);
        }

        public async Task<List<TaskPositionResponse>?> MoveTaskAsync(int boardId, int userId, int taskId, TaskPositionRequest request, CancellationToken ct)
        {
            var currentBoardUser = await _db.BoardUsers
                .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);
            if (currentBoardUser == null)
                return null;

            var movedTask = await _db.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);
            if (movedTask == null)
                return null;

            var targetStatus = await _db.Statuses
                .FirstOrDefaultAsync(s => s.StatusId == request.StatusId && s.BoardId == boardId, ct);
            if (targetStatus == null)
                return null;

            var sourceStatusId = movedTask.StatusId;
            var isStatusChanged = sourceStatusId != targetStatus.StatusId;

            var targetTasks = await _db.Tasks
                .Where(t => t.BoardId == boardId && t.StatusId == targetStatus.StatusId && t.TaskId != taskId)
                .OrderBy(t => t.Order)
                .ThenBy(t => t.TaskId)
                .ToListAsync(ct);

            var sourceTasks = new List<Task>();

            if (isStatusChanged)
            {
                sourceTasks = await _db.Tasks
                    .Where(t => t.BoardId == boardId && t.StatusId == sourceStatusId && t.TaskId != taskId)
                    .OrderBy(t => t.Order)
                    .ThenBy(t => t.TaskId)
                    .ToListAsync(ct);
            }

            var position = request.Position;
            if (position < 0)
                position = 0;
            if (position > targetTasks.Count)
                position = targetTasks.Count;

            movedTask.StatusId = targetStatus.StatusId;
            targetTasks.Insert(position, movedTask);

            for (var i = 0; i < targetTasks.Count; i++)
                targetTasks[i].Order = i;

            for (var i = 0; i < sourceTasks.Count; i++)
                sourceTasks[i].Order = i;

            if (isStatusChanged)
            {
                var history = new TaskStatusHistory
                {
                    TaskId = movedTask.TaskId,
                    StatusId = targetStatus.StatusId,
                    AuthorId = currentBoardUser.BoardUserId,
                    ChangeDate = DateTime.UtcNow
                };

                _db.TaskStatusHistories.Add(history);
            }

            await _db.SaveChangesAsync(ct);

            var affectedTasks = new List<Task>(targetTasks);
            affectedTasks.AddRange(sourceTasks);

            return affectedTasks
                .Select(t => new TaskPositionResponse
                {
                    Id = t.TaskId,
                    StatusId = t.StatusId,
                    Order = t.Order
                })
                .ToList();
        }

        public async Task<StatusHistoryResponse?> ChangeTaskStatusAsync(int boardId, int userId, int taskId, StatusHistoryRequest request ,CancellationToken ct)
        {
            var workerFromThisBoard = await _db.BoardUsers
            .Include(bu => bu.User)
            .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);

            if (workerFromThisBoard == null)
                return null;

            var changedTaskByStatus = await _db.Tasks
            .FirstOrDefaultAsync(t => t.BoardId == boardId && t.TaskId == taskId, ct);

            var newStatus = await _db.Statuses
                .Where(s => s.StatusId == request.NewStatusId && s.BoardId == boardId)
                .FirstOrDefaultAsync(ct);

            if (changedTaskByStatus == null || newStatus == null)
            {
                return null;
            }

            var maxOrder = await _db.Tasks
                .Where(t => t.StatusId == newStatus.StatusId && t.TaskId != changedTaskByStatus.TaskId)
                .MaxAsync(t => (int?)t.Order, ct) ?? -1;

            changedTaskByStatus.StatusId = newStatus.StatusId;
            changedTaskByStatus.Order = maxOrder + 1;

            var history = new TaskStatusHistory
            {
                TaskId = changedTaskByStatus.TaskId,
                StatusId = newStatus.StatusId,
                AuthorId = workerFromThisBoard.BoardUserId,
                ChangeDate = DateTime.UtcNow
            };
            _db.TaskStatusHistories.Add(history);
            await _db.SaveChangesAsync(ct);

            return new StatusHistoryResponse
            {
                StatusChangeId = history.StatusChangeId,
                TaskId = changedTaskByStatus.TaskId,
                Status = new StatusResponse
                {
                    StatusId = newStatus.StatusId,
                    StatusName = newStatus.StatusName,
                    Order = newStatus.Order
                },
                LastStatusChangeDate = history.ChangeDate,
                Author = new UserResponse
                {
                    UserId = workerFromThisBoard.UserId,
                    Login = workerFromThisBoard.User.Login
                }
            };
        }

        public async Task<bool> DeleteTaskAsync(int boardId, int userId, int taskId, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return false;

            var taskToDelete = await _db.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);
            if (taskToDelete is null)
                return false;

            var currentBoardUser = await _db.BoardUsers
                .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);
            if (currentBoardUser is null)
                return false;

            bool isAuthor = taskToDelete.AuthorId == currentBoardUser.BoardUserId;
            bool isOwner = await IsUserBoardOwnerAsync(boardId, userId, ct);

            if (!isAuthor && !isOwner)
                return false;

            _db.Tasks.Remove(taskToDelete);
            await _db.SaveChangesAsync(ct);

            return true;
        }

        public async Task<TaskResponse?> UpdateTaskAsync(int boardId, int userId, int taskId, TaskRequest request, CancellationToken ct)
        {
            var currentBoardUser = await _db.BoardUsers
                .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);
            if (currentBoardUser == null)
                return null;

            var task = await _db.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);
            if (task == null)
                return null;

            int? assigneeId = null;

            if (request.WorkerId.HasValue)
            {
                var workerFromThisBoard = await _db.BoardUsers
                    .FirstOrDefaultAsync(bu => bu.UserId == request.WorkerId.Value && bu.BoardId == boardId && !bu.IsDeleted, ct);
                if (workerFromThisBoard == null)
                    return null;

                assigneeId = workerFromThisBoard.BoardUserId;
            }

            task.TaskName = request.TaskName;
            task.TaskDescription = request.TaskDescription;
            task.DeadLine = NormalizeUtc(request.Deadline!.Value);
            task.AssigneeId = assigneeId;

            await _db.SaveChangesAsync(ct);

            return await GetTaskResponseAsync(boardId, taskId, ct);
        }

        public async Task<TaskResponse?> CreateTaskAsync(int boardId, int userId, TaskRequest request,  CancellationToken ct)
        {
            var authorFromThisBoard = await _db.BoardUsers
            .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);

            if (authorFromThisBoard == null)
                return null;

            BoardUser? workerFromThisBoard = null;

            if (request.WorkerId.HasValue)
            {
                workerFromThisBoard = await _db.BoardUsers
                    .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == request.WorkerId.Value && !bu.IsDeleted, ct);

                if (workerFromThisBoard == null)
                    return null;
            }

            var defaultStatus = await _db.Statuses
                .Where(s => s.BoardId == boardId)
                .OrderBy(s => s.Order)
                .ThenBy(s => s.StatusId)
                .FirstOrDefaultAsync(ct);

            if (defaultStatus == null)
            {
                return null;
            }

            var maxOrder = await _db.Tasks
                .Where(t => t.StatusId == defaultStatus.StatusId)
                .MaxAsync(t => (int?)t.Order, ct) ?? -1;

            var task = new Task
            {
                TaskName = request.TaskName,
                TaskDescription = request.TaskDescription,
                AssigneeId = workerFromThisBoard?.BoardUserId,
                AuthorId = authorFromThisBoard.BoardUserId,
                BoardId = boardId,
                StatusId = defaultStatus.StatusId,
                Order = maxOrder + 1,
                DeadLine = NormalizeUtc(request.Deadline!.Value),
                CreationDate = DateTime.UtcNow,
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync(ct);

            var history = new TaskStatusHistory
            {
                TaskId = task.TaskId,
                StatusId = defaultStatus.StatusId,
                AuthorId = authorFromThisBoard.BoardUserId,
                ChangeDate = task.CreationDate
            };

            _db.TaskStatusHistories.Add(history);
            await _db.SaveChangesAsync(ct);

            return await GetTaskResponseAsync(boardId, task.TaskId, ct);
        }

        public async Task<List<StatusHistoryResponse>?> GetTaskHistoryAsync(int boardId, int userId, int taskId, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return null;

            var taskFromThisBoard = await _db.Tasks
                .AnyAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);

            if (!taskFromThisBoard)
                return null;

            return await _db.TaskStatusHistories
                .Where(h => h.TaskId == taskId)
                .OrderByDescending(h => h.ChangeDate)
                .ThenByDescending(h => h.StatusChangeId)
                .Select(h => new StatusHistoryResponse
                {
                    StatusChangeId = h.StatusChangeId,
                    TaskId = h.TaskId,
                    Status = new StatusResponse
                    {
                        StatusId = h.Status.StatusId,
                        StatusName = h.Status.StatusName,
                        Order = h.Status.Order
                    },
                    LastStatusChangeDate = h.ChangeDate,
                    Author = new UserResponse
                    {
                        UserId = h.Author.UserId,
                        Login = h.Author.User.Login
                    }
                })
                .ToListAsync(ct);
        }

        public async Task<TaskResponse?> GetBoardTaskAsync(int boardId, int userId, int taskId, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return null;

            return await GetTaskResponseAsync(boardId, taskId, ct);
        }

        public async Task<List<TaskResponse>?> GetAllBoardTasksAsync(int boardId, int userId,  CancellationToken ct, int? statusId = null,  string? search = null)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return null;

            var query = _db.Tasks
                .Where(t => t.BoardId == boardId);

            if (statusId.HasValue)
                query = query.Where(t => t.StatusId == statusId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(t =>
                    t.TaskName.ToLower().Contains(lowerSearch) ||
                    (t.TaskDescription != null && t.TaskDescription.ToLower().Contains(lowerSearch)));
            }

            var tasks = await query
                .OrderBy(t => t.Order)
                .ThenBy(t => t.TaskId)
                .Select(ToTaskResponse)
                .ToListAsync(ct);

            return tasks;
        }

    }
}
