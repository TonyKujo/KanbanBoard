using System.Security.Claims;
using KanbanBoard.Models.Requests;
using KanbanBoard.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanbanBoard.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [Route("api/auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model, CancellationToken ct)
        {
            var user = await _authService.Login(model, ct);

            if (user == null)
            {
                return Conflict("Такой логин отсутствует");
            }

            await SignInUser(user);

            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [Route("api/auth/register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model, CancellationToken ct)
        {
            var user = await _authService.Register(model, ct);

            if (user == null)
            {
                return Conflict("Логин уже занят");
            }

            await SignInUser(user);

            return Ok();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("api/auth/me")]
        public async Task<IActionResult> UserInfo(CancellationToken ct)
        {
            var login = User.FindFirstValue(ClaimTypes.Name);

            if (login == null)
            {
                return BadRequest("Не поняли кто!");
            }

            var result = await _authService.GetUserInfo(login, ct);

            return Ok(result);
        }

        [HttpPost]
        [Route("api/auth/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }

        private async Task SignInUser(Models.User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }
    }
}