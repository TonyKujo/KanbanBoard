using KanbanBoard.Models.Requests;
using KanbanBoard.Models.Responses;
using KanbanBoard.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KanbanBoard.Controllers
{
    public class AuthController(AuthService authService) : Controller
    {
        public AuthService _authService { get; set; } = authService;

        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("api/auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model, CancellationToken ct)
        {
            var result = await _authService.Login(model, ct);

            if (result == null)
            {
                return Unauthorized();
            }

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(result.ClaimsPrincipal));

            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("api/auth/register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model, CancellationToken ct)
        {
            var result = await _authService.Register(model, ct);

            if (result == null)
            {
                return Unauthorized();
            }

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(result.ClaimsPrincipal));

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
    }
}
