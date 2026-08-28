using KanbanBoard.Data;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace KanbanBoard.Services
{
    public class UserService
    {
        private readonly KanbanBoardDbContext _db;

        public UserService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }

        public async Task<List<UserResponse>?> GetUsersByLoginAsync(string query, int? limit, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            var userQuery = _db.Users
                .Where(u => u.Login.ToLower().Contains(query.ToLower()))
                .Select(u => new UserResponse
                {
                    Login = u.Login,
                    UserId = u.UserId
                });


            if(limit.HasValue && limit.Value > 0)
                userQuery = userQuery.Take(limit.Value);



            return await userQuery.ToListAsync(ct);
        }
    }
}
