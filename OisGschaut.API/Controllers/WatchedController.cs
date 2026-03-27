using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OisGschaut.API.Data;
using OisGschaut.API.DTOs;
using OisGschaut.API.Models;

namespace OisGschaut.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WatchedController(AppDbContext db) : ControllerBase
{
    // GET /api/watched?userId=1            — all watched for a user
    // GET /api/watched?userId=1&mediaId=5  — watched seasons for one media item
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WatchedDto>>> Get([FromQuery] int userId, [FromQuery] int? mediaId)
    {
        var query = db.UserSeasonWatched.Where(w => w.UserId == userId);
        if (mediaId.HasValue) query = query.Where(w => w.MediaId == mediaId.Value);

        var entries = await query
            .Select(w => new WatchedDto(w.UserId, w.MediaId, w.Season, w.WatchedAt))
            .ToListAsync();

        return Ok(entries);
    }

    // POST /api/watched  — idempotent: marks season as watched, updates timestamp if already exists
    [HttpPost]
    public async Task<ActionResult<WatchedDto>> Mark([FromBody] MarkWatchedDto dto)
    {
        if (!await db.Users.AnyAsync(u => u.Id == dto.UserId))  return NotFound("User not found.");
        if (!await db.Media.AnyAsync(m => m.Id == dto.MediaId)) return NotFound("Media not found.");

        var existing = await db.UserSeasonWatched.FindAsync(dto.UserId, dto.MediaId, dto.Season);
        if (existing is not null)
        {
            existing.WatchedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Ok(new WatchedDto(existing.UserId, existing.MediaId, existing.Season, existing.WatchedAt));
        }

        var entry = new UserSeasonWatched
        {
            UserId  = dto.UserId,
            MediaId = dto.MediaId,
            Season  = dto.Season
        };
        db.UserSeasonWatched.Add(entry);
        await db.SaveChangesAsync();

        return Ok(new WatchedDto(entry.UserId, entry.MediaId, entry.Season, entry.WatchedAt));
    }

    // DELETE /api/watched?userId=1&mediaId=5&season=2
    [HttpDelete]
    public async Task<IActionResult> Unmark([FromQuery] int userId, [FromQuery] int mediaId, [FromQuery] int season)
    {
        var entry = await db.UserSeasonWatched.FindAsync(userId, mediaId, season);
        if (entry is null) return NotFound();
        db.UserSeasonWatched.Remove(entry);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
