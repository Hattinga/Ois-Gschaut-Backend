using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OisGschaut.API.Data;
using OisGschaut.API.DTOs;

namespace OisGschaut.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MediaDto>>> GetAll([FromQuery] string? search)
    {
        var query = db.Media
            .Include(m => m.MediaType)
            .Include(m => m.Genre)
            .Include(m => m.Ratings).ThenInclude(r => r.RatingSource)
            .Include(m => m.Assets).ThenInclude(a => a.AssetType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Title.Contains(search) || (m.OriginalTitle != null && m.OriginalTitle.Contains(search)));

        var media = await query
            .Select(m => ToDto(m))
            .ToListAsync();

        return Ok(media);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MediaDto>> GetById(int id)
    {
        var media = await db.Media
            .Include(m => m.MediaType)
            .Include(m => m.Genre)
            .Include(m => m.Ratings).ThenInclude(r => r.RatingSource)
            .Include(m => m.Assets).ThenInclude(a => a.AssetType)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (media is null) return NotFound();
        return Ok(ToDto(media));
    }

    [HttpGet("{id:int}/episodes")]
    public async Task<ActionResult<IEnumerable<EpisodeDto>>> GetEpisodes(int id, [FromQuery] int? season)
    {
        if (!await db.Media.AnyAsync(m => m.Id == id)) return NotFound();

        var query = db.Episodes.Where(e => e.MediaId == id);
        if (season.HasValue) query = query.Where(e => e.Season == season.Value);

        var episodes = await query
            .OrderBy(e => e.Season)
            .ThenBy(e => e.NumberInSeason)
            .Select(e => new EpisodeDto(e.Id, e.Season, e.NumberInSeason, e.Title, e.AirDate, e.Summary))
            .ToListAsync();

        return Ok(episodes);
    }

    private static MediaDto ToDto(Models.Media m) => new(
        m.Id,
        m.TmdbId,
        m.TvMazeId,
        m.Title,
        m.OriginalTitle,
        m.MediaType.Name,
        m.Genre?.Name,
        m.Plot,
        m.ReleaseDate,
        m.Status,
        m.RuntimeMin,
        m.Ratings.Select(r => new RatingDto(r.RatingSource.Name, r.Score)),
        m.Assets.Select(a => new MediaAssetDto(a.AssetType.Name, a.Url))
    );
}
