using System.Linq.Expressions;
using KanbanBoard.Models.Responses;
using Task = KanbanBoard.Models.Task;

namespace KanbanBoard.Services
{
    public static class TaskProjections
    {
        // Проекция описана как Expression, а не обычным методом, потому что EF Core
        // переводит её в SQL: джойны и подзапросы-счётчики выполняются в базе.
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

        public static IQueryable<TaskResponse> SelectTaskResponse(this IQueryable<Task> query)
        {
            return query.Select(ToTaskResponse);
        }
    }
}
