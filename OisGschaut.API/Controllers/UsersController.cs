using Microsoft.AspNetCore.Authorization;
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
    public async Task<ActionResult<IEnumerable<UserPublicDto>>> GetAll([FromQuery] string? search)
    {
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => EF.Functions.Like(u.Username, $"%{search}%"));

        var users = await query
            .OrderBy(u => u.Username)
            .Select(u => new UserPublicDto(u.Id, u.Username))
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

    // GET /api/users/{id}/profile — stats + lists + recent watched
    [HttpGet("{id:int}/profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();

        var lists = await db.Lists
            .Where(l => l.UserId == id)
            .Select(l => new ListSummaryDto(
                l.Id, l.Name, l.Description, l.IsPublic,
                l.Items.Count,
                l.Items
                    .OrderBy(li => li.SortOrder).ThenBy(li => li.AddedAt)
                    .SelectMany(li => li.Media.Assets
                        .Where(a => a.AssetType.Name == "Poster")
                        .Select(a => a.Url)
                        .Take(1))
                    .Take(4)))
            .ToListAsync();

        var filmsWatched = await db.UserSeasonWatched
            .Where(w => w.UserId == id)
            .Select(w => w.MediaId)
            .Distinct()
            .CountAsync();

        var episodesWatched = await db.UserEpisodeWatched
            .Where(w => w.UserId == id)
            .CountAsync();

        // Fetch raw genre data into memory first, then aggregate client-side
        // (EF Core can't translate OrderByDescending on a projected record type)
        var watchedWithGenre = await db.UserSeasonWatched
            .Where(w => w.UserId == id)
            .Where(w => w.Media.Genre != null)
            .Select(w => new { w.MediaId, GenreName = w.Media.Genre!.Name })
            .ToListAsync();

        var genreBreakdown = watchedWithGenre
            .GroupBy(w => w.GenreName)
            .Select(g => new GenreBreakdownDto(g.Key, g.Select(w => w.MediaId).Distinct().Count()))
            .OrderByDescending(g => g.Count)
            .Take(8)
            .ToList();

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
            user.Id, user.Username, user.Bio, user.CreatedAt,
            lists.Count, filmsWatched, episodesWatched, genreBreakdown, lists, recentWatched));
    }

    // PUT /api/users/{id} — update username
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();

        var taken = await db.Users.AnyAsync(u => u.Username == dto.Username && u.Id != id);
        if (taken) return Conflict(new { message = "Username is already taken." });

        user.Username = dto.Username;
        if (dto.Bio is not null) user.Bio = dto.Bio;
        await db.SaveChangesAsync();
        return Ok(new UserDto(user.Id, user.Email, user.Username, user.OAuthProvider, user.CreatedAt));
    }

    [Authorize]
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
