using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Requests;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace KanbanBoard.Services
{
    public class AuthService
    {
        private readonly KanbanBoardDbContext _db;

        public AuthService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }

        public async Task<User?> Register(RegisterRequest request, CancellationToken ct)
        {
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Login == request.Login, ct);

            if (existingUser is not null)
            {
                return null; 
            }

            var user = new User()
            {
                Login = request.Login,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                DateOfRegistration = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            return user; 
        }

        public async Task<User?> Login(LoginRequest request, CancellationToken ct)
        {
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Login == request.Login, ct);

            if (existingUser is null || !BCrypt.Net.BCrypt.Verify(request.Password, existingUser.PasswordHash))
            {
                return null; 
            }

            return existingUser; 
        }

        public async Task<GetUserInfoRespones> GetUserInfo(string login, CancellationToken ct)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == login, ct)
                       ?? throw new InvalidOperationException("Неверный логин!");

            return new GetUserInfoRespones()
            {
                UserId = user.UserId,
                Login = user.Login,
                DateOfRegistration = user.DateOfRegistration,
            };
        }
    }
}