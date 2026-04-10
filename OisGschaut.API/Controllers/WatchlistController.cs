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
[Authorize]
public class WatchlistController(AppDbContext db) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<UserList> GetOrCreateWatchlistAsync(int userId)
    {
        var list = await db.Lists
            .FirstOrDefaultAsync(l => l.UserId == userId && l.Name == "Watchlist" && !l.IsPublic);

        if (list is not null) return list;

        list = new UserList
        {
            UserId      = userId,
            Name        = "Watchlist",
            Description = "Films I want to watch",
            IsPublic    = false
        };
        db.Lists.Add(list);
        await db.SaveChangesAsync();
        return list;
    }

    // GET /api/watchlist — returns mediaIds saved in user's watchlist
    [HttpGet]
    public async Task<ActionResult<IEnumerable<int>>> Get()
    {
        var userId = CurrentUserId;
        var list = await db.Lists
            .FirstOrDefaultAsync(l => l.UserId == userId && l.Name == "Watchlist" && !l.IsPublic);

        if (list is null) return Ok(Array.Empty<int>());

        var ids = await db.ListItems
            .Where(li => li.ListId == list.Id)
            .Select(li => li.MediaId)
            .ToListAsync();

        return Ok(ids);
    }

    // POST /api/watchlist/toggle { mediaId } — adds or removes from watchlist
    [HttpPost("toggle")]
    public async Task<ActionResult<WatchlistToggleDto>> Toggle([FromBody] WatchlistToggleRequestDto dto)
    {
        var userId = CurrentUserId;

        var media = await db.Media.FindAsync(dto.MediaId);
        if (media is null) return NotFound("Media not found.");

        var list = await GetOrCreateWatchlistAsync(userId);
        var existing = await db.ListItems
            .FirstOrDefaultAsync(li => li.ListId == list.Id && li.MediaId == dto.MediaId);

        if (existing is not null)
        {
            db.ListItems.Remove(existing);
            await db.SaveChangesAsync();
            return Ok(new WatchlistToggleDto(dto.MediaId, false));
        }

        db.ListItems.Add(new ListItem
        {
            ListId      = list.Id,
            MediaId     = dto.MediaId,
            MediaTypeId = media.MediaTypeId
        });
        await db.SaveChangesAsync();
        return Ok(new WatchlistToggleDto(dto.MediaId, true));
    }
}
