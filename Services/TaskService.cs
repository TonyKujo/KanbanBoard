using KanbanBoard.Data;
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

        public async Task<TaskResponse> CreateTaskAsync(int boardId, int userId, TaskRequest request,  CancellationToken ct)
        {
            var workerFromThisBoard = await _db.BoardUsers
            .FirstOrDefaultAsync(bu => bu.UserId == request.WorkerId && bu.BoardId == boardId);

            var authorFromThisBoard = await _db.BoardUsers
            .FirstOrDefaultAsync(bu => bu.UserId == userId && bu.BoardId == boardId, ct);

            if (workerFromThisBoard == null || authorFromThisBoard == null)
                return null;

            var task = new Task
            {
                TaskName = request.TaskName,
                TaskDescription = request.TaskDescription,
                AssigneeId = workerFromThisBoard.BoardUserId,
                AuthorId = authorFromThisBoard.BoardUserId,
                BoardId = boardId,
                DeadLine = request.Deadline,
                CreationDate = DateTime.UtcNow,
            };

            

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            var createdTask = await _db.Tasks
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

            //Нужно добавить сюды установку статуса. Но прежде чем - реализовать полный функционал с ними, чтобы было что добавлять
        }

        public async Task<List<TaskResponse>> GetAllBoardTasksAsync(int boardId, CancellationToken ct)
        {
            var tasks = await _db.Tasks
                .Where(t => t.BoardId == boardId)
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
