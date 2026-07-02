using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Requests;
using KanbanBoard.Models.Responses;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Task = System.Threading.Tasks.Task;

namespace KanbanBoard.Services
{
    public class AuthService
    {
        private readonly KanbanBoardDbContext _db;
        public AuthService(KanbanBoardDbContext dbContext) 
        { 
            _db = dbContext; 
        }

        public async Task<AuthResponse> Register(RegisterRequest request, CancellationToken ct)
        {
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Login == request.Login, ct);

            if (existingUser is not null)
            {
                throw new InvalidOperationException("Пользователь с таким логином уже существует!");
            }

            var user = new User()
            {
                Login = request.Login,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _db.Users.Add(user);

            await _db.SaveChangesAsync(ct);

            return CreatePrincipal(user);
        }

        public async Task<AuthResponse> Login(LoginRequest request, CancellationToken ct) 
        {
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Login == request.Login, ct);

            if (existingUser is null || !BCrypt.Net.BCrypt.Verify(request.Password, existingUser.PasswordHash)) 
            {
                throw new InvalidOperationException("Неверный логин или пароль!");
                
            }

            return CreatePrincipal(existingUser);
        }

        private static AuthResponse CreatePrincipal(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Login)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            return new AuthResponse
            {
                ClaimsPrincipal = principal,
            };
        }
    }
}
