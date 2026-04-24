using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<WatchedDto>> Mark([FromBody] MarkWatchedDto dto)
    {
        var userId = CurrentUserId;
        if (!await db.Media.AnyAsync(m => m.Id == dto.MediaId)) return NotFound("Media not found.");

        var existing = await db.UserSeasonWatched.FindAsync(userId, dto.MediaId, dto.Season);
        if (existing is not null)
        {
            existing.WatchedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Ok(new WatchedDto(existing.UserId, existing.MediaId, existing.Season, existing.WatchedAt));
        }

        var entry = new UserSeasonWatched
        {
            UserId  = userId,
            MediaId = dto.MediaId,
            Season  = dto.Season
        };
        db.UserSeasonWatched.Add(entry);
        await db.SaveChangesAsync();

        return Ok(new WatchedDto(entry.UserId, entry.MediaId, entry.Season, entry.WatchedAt));
    }

    // DELETE /api/watched?mediaId=5&season=2
    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> Unmark([FromQuery] int mediaId, [FromQuery] int season)
    {
        var entry = await db.UserSeasonWatched.FindAsync(CurrentUserId, mediaId, season);
        if (entry is null) return NotFound();
        db.UserSeasonWatched.Remove(entry);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // GET /api/watched/episodes?userId=1&mediaId=5 — returns watched episode IDs for a media item
    [HttpGet("episodes")]
    public async Task<ActionResult<IEnumerable<int>>> GetWatchedEpisodes(
        [FromQuery] int userId, [FromQuery] int mediaId)
    {
        var episodeIds = await db.UserEpisodeWatched
            .Where(w => w.UserId == userId && w.Episode.MediaId == mediaId)
            .Select(w => w.EpisodeId)
            .ToListAsync();
        return Ok(episodeIds);
    }

    // POST /api/watched/episodes — mark episode as watched (idempotent)
    [Authorize]
    [HttpPost("episodes")]
    public async Task<IActionResult> MarkEpisode([FromBody] MarkEpisodeWatchedDto dto)
    {
        var userId = CurrentUserId;
        if (!await db.Episodes.AnyAsync(e => e.Id == dto.EpisodeId))
            return NotFound("Episode not found.");

        var existing = await db.UserEpisodeWatched.FindAsync(userId, dto.EpisodeId);
        if (existing is not null) return Ok();

        db.UserEpisodeWatched.Add(new UserEpisodeWatched { UserId = userId, EpisodeId = dto.EpisodeId });
        await db.SaveChangesAsync();
        return Ok();
    }

    // DELETE /api/watched/episodes?episodeId=42 — unmark episode as watched
    [Authorize]
    [HttpDelete("episodes")]
    public async Task<IActionResult> UnmarkEpisode([FromQuery] int episodeId)
    {
        var entry = await db.UserEpisodeWatched.FindAsync(CurrentUserId, episodeId);
        if (entry is null) return NotFound();
        db.UserEpisodeWatched.Remove(entry);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
