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

        private async Task<bool> IsUserBoardMemberAsync(int boardId, int userId, CancellationToken ct)
        {
            return await _db.BoardUsers
                .AnyAsync(bu => bu.BoardId == boardId && bu.UserId == userId, ct);

            
        }

        private async Task<bool> IsUserBoardOwnerAsync(int boardId, int userId, CancellationToken ct)
        {
            return await _db.Boards.AnyAsync(b => b.BoardId == boardId && b.AuthorId == userId, ct);
        }

        public async Task<StatusHistoryResponse?> ChangeTaskStatusAsync(int boardId, int userId, int taskId, StatusHistoryRequest request ,CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
            {
                return null;
            }

            var workerFromThisBoard = await _db.BoardUsers
            .FirstOrDefaultAsync(bu => bu.UserId == userId && bu.BoardId == boardId, ct);

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

            changedTaskByStatus.StatusId = newStatus.StatusId;

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
                TaskId = changedTaskByStatus.TaskId,
                Status = new StatusResponse
                {
                    StatusId = newStatus.StatusId,
                    StatusName = newStatus.StatusName
                },
                LastStatusChangeDate = history.ChangeDate
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
                .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId, ct);
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
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
            {
                return null;
            }

            var workerFromThisBoard = await _db.BoardUsers
            .FirstOrDefaultAsync(bu => bu.UserId == request.WorkerId && bu.BoardId == boardId, ct);

            if (workerFromThisBoard == null)
                return null;

            var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);

            if (task is null)
                return null;

            task.TaskName = request.TaskName;

            task.TaskDescription = request.TaskDescription;
            task.DeadLine = request.Deadline;
            task.Assignee = workerFromThisBoard;

            await _db.SaveChangesAsync(ct);

            var updatedTask = await _db.Tasks
            .Include(t => t.Status)
            .Include(t => t.Author).ThenInclude(bu => bu.User)
            .Include(t => t.Assignee).ThenInclude(bu => bu.User)
            .FirstAsync(t => t.TaskId == taskId, ct);

            return new TaskResponse
            {
                TaskId = updatedTask.TaskId,
                TaskName = updatedTask.TaskName,
                TaskDescription = updatedTask.TaskDescription,
                Deadline = updatedTask.DeadLine,
                DateOfMade = updatedTask.CreationDate,
                Status = new StatusResponse
                {
                    StatusId = updatedTask.Status.StatusId,
                    StatusName = updatedTask.Status.StatusName
                },
                Author = new UserResponse
                {
                    UserId = updatedTask.Author.UserId,
                    Login = updatedTask.Author.User.Login
                },
                Worker = new UserResponse
                {
                    UserId = updatedTask.Assignee.UserId,
                    Login = updatedTask.Assignee.User.Login
                }

            };
        }

        public async Task<TaskResponse?> CreateTaskAsync(int boardId, int userId, TaskRequest request,  CancellationToken ct)
        {
            if(!await IsUserBoardMemberAsync(boardId, userId, ct))
            {
                return null;
            }


            var workerFromThisBoard = await _db.BoardUsers
            .FirstOrDefaultAsync(bu => bu.UserId == request.WorkerId && bu.BoardId == boardId, ct);

            var authorFromThisBoard = await _db.BoardUsers
            .FirstOrDefaultAsync(bu => bu.UserId == userId && bu.BoardId == boardId, ct);

            if (workerFromThisBoard == null || authorFromThisBoard == null)
                return null;

            var defaultStatus = await _db.Statuses
                .Where(s => s.BoardId == boardId)
                .OrderBy(s => s.StatusId)
                .FirstOrDefaultAsync(ct);

            if (defaultStatus == null)
            {
                return null;
            }

            var task = new Task
            {
                TaskName = request.TaskName,
                TaskDescription = request.TaskDescription,
                AssigneeId = workerFromThisBoard.BoardUserId,
                AuthorId = authorFromThisBoard.BoardUserId,
                BoardId = boardId,
                StatusId = defaultStatus.StatusId,
                DeadLine = request.Deadline,
                CreationDate = DateTime.UtcNow,
            };

            

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync(ct);

            var createdTask = await _db.Tasks
            .Include(t => t.Status)
            .Include(t => t.Author).ThenInclude(bu => bu.User)
            .Include(t => t.Assignee).ThenInclude(bu => bu.User)
            .FirstAsync(t => t.TaskId == task.TaskId, ct);

            return new TaskResponse
            {
                TaskId = createdTask.TaskId,
                TaskName = createdTask.TaskName,
                TaskDescription = createdTask.TaskDescription,
                Deadline = createdTask.DeadLine,
                DateOfMade = createdTask.CreationDate,
                Status = new StatusResponse
                {
                    StatusId = createdTask.Status.StatusId,
                    StatusName = createdTask.Status.StatusName
                },
                Author = new UserResponse
                {
                    UserId = createdTask.Author.UserId,
                    Login = createdTask.Author.User.Login
                },
                Worker = new UserResponse
                {
                    UserId = createdTask.Assignee.UserId,
                    Login = createdTask.Assignee.User.Login
                }

            };

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
                .Select(t => new TaskResponse
                {
                    TaskId = t.TaskId,
                    TaskName = t.TaskName,
                    TaskDescription = t.TaskDescription,
                    Deadline = t.DeadLine,
                    DateOfMade = t.CreationDate,
                    Status = new StatusResponse
                    {
                        StatusId = t.Status.StatusId,
                        StatusName = t.Status.StatusName
                    },
                    Worker = new UserResponse
                    {
                        UserId = t.Assignee.UserId,
                        Login = t.Assignee.User.Login,
                    },
                    Author = new UserResponse
                    {
                        UserId = t.Author.UserId,
                        Login = t.Author.User.Login,
                    }
                })
                .ToListAsync(ct);

            return tasks;
        }

    }
}
