using KanbanBoard.Data;
using KanbanBoard.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Task = System.Threading.Tasks.Task;

namespace KanbanBoard.Services
{
    public class UserService
    {
        private readonly KanbanBoardDbContext _db;
        public UserService(KanbanBoardDbContext dbContext) { _db = dbContext; }


        public void Register(string login, string password)
        {
            var userFromDb = _db.Users.FirstOrDefault(u => u.Login == login);
            if (userFromDb != null){
                throw new InvalidOperationException("Пользователь с таким логином уже существует!");
            }
            else
            {
                User User = new User();
                User.Login = login;
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                User.Password = passwordHash;
                _db.Users.Add(User);
                _db.SaveChanges();
            }
            
        }
        public async Task Login(string login, string password, HttpContext httpContext) {
            var UserFromDb = _db.Users.FirstOrDefault(u => u.Login == login);
            if (UserFromDb == null || !BCrypt.Net.BCrypt.Verify(password, UserFromDb.Password)) {
                throw new InvalidOperationException("Неверный логин или пароль!");
                
            }
            else {
                    var claims = new List<Claim> { new Claim(ClaimTypes.Name, UserFromDb.Login) };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await httpContext.SignInAsync(principal);
            }
        }
    }
}
