using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OisGschaut.API.Data;
using OisGschaut.API.DTOs;
using OisGschaut.API.Services;

namespace OisGschaut.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController(AppDbContext db, MediaSyncService sync) : ControllerBase
{
    // GET /api/media?search=x  — search local DB
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
            query = query.Where(m =>
                m.Title.Contains(search) ||
                (m.OriginalTitle != null && m.OriginalTitle.Contains(search)));

        var media = await query.Select(m => ToDto(m)).ToListAsync();
        return Ok(media);
    }

    // GET /api/media/{id}
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

    // GET /api/media/{id}/episodes?season=1
    [HttpGet("{id:int}/episodes")]
    public async Task<ActionResult<IEnumerable<EpisodeDto>>> GetEpisodes(int id, [FromQuery] int? season)
    {
        if (!await db.Media.AnyAsync(m => m.Id == id)) return NotFound();

        var query = db.Episodes.Where(e => e.MediaId == id);
        if (season.HasValue) query = query.Where(e => e.Season == season.Value);

        var episodes = await query
            .OrderBy(e => e.Season).ThenBy(e => e.NumberInSeason)
            .Select(e => new EpisodeDto(e.Id, e.Season, e.NumberInSeason, e.Title, e.AirDate, e.Summary))
            .ToListAsync();

        return Ok(episodes);
    }

    // GET /api/media/search?q=inception  — search TMDB, returns results without persisting
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<TmdbSearchResultDto>>> SearchTmdb([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("Query parameter 'q' is required.");
        var results = await sync.SearchTmdbAsync(q);
        return Ok(results);
    }

    // POST /api/media/import  — import a title from TMDB into DB (idempotent)
    [HttpPost("import")]
    public async Task<ActionResult<MediaDto>> Import([FromBody] ImportMediaDto dto)
    {
        if (dto.Type is not "movie" and not "tv")
            return BadRequest("Type must be 'movie' or 'tv'.");

        var media = await sync.ImportFromTmdbAsync(dto.TmdbId, dto.Type);
        if (media is null) return NotFound("TMDB title not found.");

        var full = await db.Media
            .Include(m => m.MediaType)
            .Include(m => m.Genre)
            .Include(m => m.Ratings).ThenInclude(r => r.RatingSource)
            .Include(m => m.Assets).ThenInclude(a => a.AssetType)
            .FirstAsync(m => m.Id == media.Id);

        return Ok(ToDto(full));
    }

    // POST /api/media/{id}/sync-episodes?tvMazeId=82  — sync episodes from TVMaze
    // If tvMazeId is omitted and not stored, auto-searches TVMaze by title.
    [HttpPost("{id:int}/sync-episodes")]
    public async Task<IActionResult> SyncEpisodes(int id, [FromQuery] int? tvMazeId, [FromServices] TvMazeService tvMaze)
    {
        var media = await db.Media.FindAsync(id);
        if (media is null) return NotFound();

        var mazeId = tvMazeId ?? media.TvMazeId;

        // Auto-search TVMaze by title when no ID is available
        if (mazeId is null)
        {
            var found = await tvMaze.SearchShowAsync(media.Title);
            if (found is null)
                return NotFound("Could not find this show on TVMaze. Pass tvMazeId manually.");
            mazeId = found.Id;
        }

        // Persist the TvMazeId for future syncs
        if (media.TvMazeId is null)
        {
            media.TvMazeId = mazeId;
            await db.SaveChangesAsync();
        }

        await sync.SyncEpisodesAsync(id, mazeId.Value);
        return NoContent();
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
