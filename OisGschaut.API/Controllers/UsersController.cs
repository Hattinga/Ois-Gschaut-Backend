using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OisGschaut.API.Data;
using OisGschaut.API.DTOs;
using OisGschaut.API.Models;

namespace OisGschaut.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await db.Users
            .Select(u => new UserDto(u.Id, u.Email, u.Username, u.OAuthProvider, u.CreatedAt))
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        return Ok(new UserDto(user.Id, user.Email, user.Username, user.OAuthProvider, user.CreatedAt));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserDto dto)
    {
        var user = new User
        {
            Email         = dto.Email,
            Username      = dto.Username,
            OAuthProvider = dto.OAuthProvider,
            OAuthId       = dto.OAuthId
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var result = new UserDto(user.Id, user.Email, user.Username, user.OAuthProvider, user.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, result);
    }

    // Find-or-create a guest user by username (pre-auth convenience endpoint)
    [HttpPost("guest")]
    public async Task<ActionResult<UserDto>> GuestLogin([FromBody] GuestLoginDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (user is null)
        {
            user = new User
            {
                Email    = $"{dto.Username.ToLower().Replace(" ", "_")}@guest.oisgschaut.local",
                Username = dto.Username
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        return Ok(new UserDto(user.Id, user.Email, user.Username, user.OAuthProvider, user.CreatedAt));
    }

    // GET /api/users/{id}/profile — stats + lists + recent watched
    [HttpGet("{id:int}/profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();

        var lists = await db.Lists
            .Where(l => l.UserId == id)
            .Select(l => new ListSummaryDto(l.Id, l.Name, l.Description, l.IsPublic, l.Items.Count))
            .ToListAsync();

        var filmsWatched = await db.UserSeasonWatched
            .Where(w => w.UserId == id)
            .Select(w => w.MediaId)
            .Distinct()
            .CountAsync();

        var recentWatched = await db.UserSeasonWatched
            .Where(w => w.UserId == id)
            .Include(w => w.Media).ThenInclude(m => m.Assets).ThenInclude(a => a.AssetType)
            .OrderByDescending(w => w.WatchedAt)
            .Take(24)
            .Select(w => new WatchedMediaDto(
                w.MediaId,
                w.Media.Title,
                w.Media.Assets
                    .Where(a => a.AssetType.Name == "Poster")
                    .Select(a => a.Url)
                    .FirstOrDefault(),
                w.Season,
                w.WatchedAt))
            .ToListAsync();

        return Ok(new UserProfileDto(
            user.Id, user.Username, user.CreatedAt,
            lists.Count, filmsWatched, lists, recentWatched));
    }

    // PUT /api/users/{id} — update username
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();

        var taken = await db.Users.AnyAsync(u => u.Username == dto.Username && u.Id != id);
        if (taken) return Conflict(new { message = "Username is already taken." });

        user.Username = dto.Username;
        await db.SaveChangesAsync();
        return Ok(new UserDto(user.Id, user.Email, user.Username, user.OAuthProvider, user.CreatedAt));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
