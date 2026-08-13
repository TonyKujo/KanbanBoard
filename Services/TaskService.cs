using KanbanBoard.Data;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace KanbanBoard.Services
{
    public class TaskService
    {
        private readonly KanbanBoardDbContext _db;

        public TaskService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }

        public async Task<List<TaskResponse>> GetAllBoardTasksAsync(int boardId, CancellationToken ct)
        {
            var tasks = await _db.Tasks
                .Where(t => t.BoardId == boardId)
                .Select(t => new TaskResponse
                {
                    TaskId = t.TaskId,
                    NameOfTask = t.TaskName,
                    DescriptionOfTask = t.TaskDescription,
                    Deadline = t.DeadLine,
                    DateOfMade = t.CreationDate,
                    Status = new StatusResponse
                    {
                        StatusId = t.Status.StatusId,
                        NameOfStatus = t.Status.StatusName
                    },
                    Worker = new UserResponse
                    {
                        UserId = t.Assignee.UserId,
                        Login = t.Assignee.User.Login,
                    }

                })
                .ToListAsync(ct);

            return tasks;
        }

    }
}
