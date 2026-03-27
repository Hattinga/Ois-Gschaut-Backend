using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OisGschaut.API.Data;
using OisGschaut.API.DTOs;
using OisGschaut.API.Models;
using OisGschaut.API.Services;

namespace OisGschaut.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext db, JwtService jwt, IConfiguration config) : ControllerBase
{
    // POST /api/auth/guest — find-or-create guest user, return JWT
    [HttpPost("guest")]
    public async Task<ActionResult<AuthResponseDto>> GuestLogin([FromBody] GuestLoginDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (user is null)
        {
            user = new User
            {
                Email    = $"{dto.Username.ToLower().Replace(" ", "_")}@guest.oisgschaut.local",
                Username = dto.Username,
                OAuthProvider = "guest"
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var token   = jwt.GenerateToken(user);
        var userDto = new UserDto(user.Id, user.Email, user.Username, user.OAuthProvider, user.CreatedAt);
        return Ok(new AuthResponseDto(userDto, token));
    }

    // GET /api/auth/google — redirect to Google OAuth (not yet configured)
    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        var clientId = config["OAuth:Google:ClientId"];
        if (string.IsNullOrEmpty(clientId))
            return StatusCode(501, new { message = "Google OAuth is not configured yet." });

        var callback = Uri.EscapeDataString(config["OAuth:Google:CallbackUrl"] ?? "");
        var url = $"https://accounts.google.com/o/oauth2/v2/auth" +
                  $"?client_id={clientId}&redirect_uri={callback}" +
                  $"&response_type=code&scope=openid%20email%20profile";
        return Redirect(url);
    }

    // GET /api/auth/github — redirect to GitHub OAuth (not yet configured)
    [HttpGet("github")]
    public IActionResult GitHubLogin()
    {
        var clientId = config["OAuth:GitHub:ClientId"];
        if (string.IsNullOrEmpty(clientId))
            return StatusCode(501, new { message = "GitHub OAuth is not configured yet." });

        var callback = Uri.EscapeDataString(config["OAuth:GitHub:CallbackUrl"] ?? "");
        var url = $"https://github.com/login/oauth/authorize" +
                  $"?client_id={clientId}&redirect_uri={callback}&scope=user:email";
        return Redirect(url);
    }

    // GET /api/auth/callback/google — OAuth callback stub
    [HttpGet("callback/google")]
    public IActionResult GoogleCallback([FromQuery] string? code, [FromQuery] string? error)
    {
        var frontendUrl = config["OAuth:FrontendUrl"] ?? "http://localhost:3001";
        if (!string.IsNullOrEmpty(error))
            return Redirect($"{frontendUrl}?auth_error={Uri.EscapeDataString(error)}");

        // TODO: exchange code for tokens, look up / create user, generate JWT
        return Redirect($"{frontendUrl}?auth_error=not_implemented");
    }

    // GET /api/auth/callback/github — OAuth callback stub
    [HttpGet("callback/github")]
    public IActionResult GitHubCallback([FromQuery] string? code, [FromQuery] string? error)
    {
        var frontendUrl = config["OAuth:FrontendUrl"] ?? "http://localhost:3001";
        if (!string.IsNullOrEmpty(error))
            return Redirect($"{frontendUrl}?auth_error={Uri.EscapeDataString(error)}");

        // TODO: exchange code for tokens, look up / create user, generate JWT
        return Redirect($"{frontendUrl}?auth_error=not_implemented");
    }
}
