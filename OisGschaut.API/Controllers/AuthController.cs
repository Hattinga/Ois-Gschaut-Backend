using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OisGschaut.API.Data;
using OisGschaut.API.DTOs;
using OisGschaut.API.Models;
using OisGschaut.API.Services;

namespace OisGschaut.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext db, JwtService jwt, IConfiguration config, HttpClient http) : ControllerBase
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

    // GET /api/auth/me — return current user from JWT
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user   = await db.Users.FindAsync(userId);
        if (user is null) return NotFound();
        return Ok(new UserDto(user.Id, user.Email, user.Username, user.OAuthProvider, user.CreatedAt));
    }

    // GET /api/auth/google — redirect to Google OAuth
    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        var clientId = config["OAuth:Google:ClientId"];
        if (string.IsNullOrEmpty(clientId))
            return StatusCode(501, new { message = "Google OAuth is not configured." });

        var callback = Uri.EscapeDataString(config["OAuth:Google:CallbackUrl"] ?? "");
        var url = "https://accounts.google.com/o/oauth2/v2/auth" +
                  $"?client_id={clientId}&redirect_uri={callback}" +
                  "&response_type=code&scope=openid%20email%20profile";
        return Redirect(url);
    }

    // GET /api/auth/github — redirect to GitHub OAuth
    [HttpGet("github")]
    public IActionResult GitHubLogin()
    {
        var clientId = config["OAuth:GitHub:ClientId"];
        if (string.IsNullOrEmpty(clientId))
            return StatusCode(501, new { message = "GitHub OAuth is not configured." });

        var callback = Uri.EscapeDataString(config["OAuth:GitHub:CallbackUrl"] ?? "");
        var url = "https://github.com/login/oauth/authorize" +
                  $"?client_id={clientId}&redirect_uri={callback}&scope=user:email";
        return Redirect(url);
    }

    // GET /api/auth/callback/google
    [HttpGet("callback/google")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? code, [FromQuery] string? error)
    {
        var frontendUrl = config["OAuth:FrontendUrl"] ?? "http://localhost:3001";
        if (!string.IsNullOrEmpty(error))
            return Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString(error)}");
        if (string.IsNullOrEmpty(code))
            return Redirect($"{frontendUrl}/auth/callback?error=no_code");

        // 1. Exchange code for access token
        var tokenResp = await http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"]          = code,
                ["client_id"]     = config["OAuth:Google:ClientId"]!,
                ["client_secret"] = config["OAuth:Google:ClientSecret"]!,
                ["redirect_uri"]  = config["OAuth:Google:CallbackUrl"]!,
                ["grant_type"]    = "authorization_code",
            }));

        if (!tokenResp.IsSuccessStatusCode)
            return Redirect($"{frontendUrl}/auth/callback?error=token_exchange_failed");

        var tokenJson   = await tokenResp.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenJson.GetProperty("access_token").GetString()!;

        // 2. Fetch user info
        using var infoReq = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
        infoReq.Headers.Authorization = new("Bearer", accessToken);
        var infoResp = await http.SendAsync(infoReq);

        if (!infoResp.IsSuccessStatusCode)
            return Redirect($"{frontendUrl}/auth/callback?error=userinfo_failed");

        var info       = await infoResp.Content.ReadFromJsonAsync<JsonElement>();
        var providerId = info.GetProperty("sub").GetString()!;
        var email      = info.GetProperty("email").GetString()!;
        var name       = info.TryGetProperty("name", out var n) && n.ValueKind != JsonValueKind.Null
                         ? n.GetString() : email.Split('@')[0];

        var user = await FindOrCreateOAuthUserAsync("google", providerId, email, name ?? email);
        return RedirectToFrontend(frontendUrl, user);
    }

    // GET /api/auth/callback/github
    [HttpGet("callback/github")]
    public async Task<IActionResult> GitHubCallback([FromQuery] string? code, [FromQuery] string? error)
    {
        var frontendUrl = config["OAuth:FrontendUrl"] ?? "http://localhost:3001";
        if (!string.IsNullOrEmpty(error))
            return Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString(error)}");
        if (string.IsNullOrEmpty(code))
            return Redirect($"{frontendUrl}/auth/callback?error=no_code");

        // 1. Exchange code for access token
        using var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");
        tokenReq.Headers.Accept.Add(new("application/json"));
        tokenReq.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"]          = code,
            ["client_id"]     = config["OAuth:GitHub:ClientId"]!,
            ["client_secret"] = config["OAuth:GitHub:ClientSecret"]!,
        });

        var tokenResp = await http.SendAsync(tokenReq);
        if (!tokenResp.IsSuccessStatusCode)
            return Redirect($"{frontendUrl}/auth/callback?error=token_exchange_failed");

        var tokenJson   = await tokenResp.Content.ReadFromJsonAsync<JsonElement>();
        if (!tokenJson.TryGetProperty("access_token", out var atProp))
            return Redirect($"{frontendUrl}/auth/callback?error=token_exchange_failed");
        var accessToken = atProp.GetString()!;

        // 2. Fetch user info
        using var infoReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        infoReq.Headers.Authorization = new("Bearer", accessToken);
        infoReq.Headers.UserAgent.ParseAdd("OisGschaut/1.0");
        var infoResp = await http.SendAsync(infoReq);

        if (!infoResp.IsSuccessStatusCode)
            return Redirect($"{frontendUrl}/auth/callback?error=userinfo_failed");

        var info       = await infoResp.Content.ReadFromJsonAsync<JsonElement>();
        var providerId = info.GetProperty("id").GetInt32().ToString();
        var login      = info.TryGetProperty("login", out var l) ? l.GetString() : null;

        // GitHub email can be null — fetch from emails endpoint
        string email;
        if (info.TryGetProperty("email", out var eProp) && eProp.ValueKind != JsonValueKind.Null)
        {
            email = eProp.GetString()!;
        }
        else
        {
            using var emailReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
            emailReq.Headers.Authorization = new("Bearer", accessToken);
            emailReq.Headers.UserAgent.ParseAdd("OisGschaut/1.0");
            var emailResp = await http.SendAsync(emailReq);
            var emails    = await emailResp.Content.ReadFromJsonAsync<JsonElement>();
            var primary   = emails.EnumerateArray()
                .FirstOrDefault(x => x.TryGetProperty("primary", out var p) && p.GetBoolean());
            email = primary.ValueKind != JsonValueKind.Undefined &&
                    primary.TryGetProperty("email", out var em)
                    ? em.GetString()! : $"{login ?? providerId}@github.local";
        }

        var user = await FindOrCreateOAuthUserAsync("github", providerId, email, login ?? email);
        return RedirectToFrontend(frontendUrl, user);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<User> FindOrCreateOAuthUserAsync(
        string provider, string providerId, string email, string username)
    {
        // 1. Existing OAuth user?
        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.OAuthProvider == provider && u.OAuthId == providerId);
        if (user is not null) return user;

        // 2. Email already registered (e.g. as guest)? Upgrade.
        user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is not null)
        {
            user.OAuthProvider = provider;
            user.OAuthId       = providerId;
            await db.SaveChangesAsync();
            return user;
        }

        // 3. New user — ensure unique username
        var finalUsername = username;
        var suffix = 1;
        while (await db.Users.AnyAsync(u => u.Username == finalUsername))
            finalUsername = $"{username}{suffix++}";

        user = new User
        {
            Email         = email,
            Username      = finalUsername,
            OAuthProvider = provider,
            OAuthId       = providerId,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private IActionResult RedirectToFrontend(string frontendUrl, User user)
    {
        var token    = jwt.GenerateToken(user);
        var userJson = Uri.EscapeDataString(JsonSerializer.Serialize(new
        {
            id            = user.Id,
            email         = user.Email,
            username      = user.Username,
            oAuthProvider = user.OAuthProvider,
            createdAt     = user.CreatedAt,
        }));
        return Redirect($"{frontendUrl}/auth/callback?token={token}&user={userJson}");
    }
}
