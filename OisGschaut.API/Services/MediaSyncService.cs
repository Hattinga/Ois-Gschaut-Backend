using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OisGschaut.API.Data;
using OisGschaut.API.DTOs;
using OisGschaut.API.Models;

namespace OisGschaut.API.Services;

public class MediaSyncService(AppDbContext db, TmdbService tmdb, TvMazeService tvMaze, IConfiguration config)
{
    private string ImageBase => config["Tmdb:ImageBase"] ?? "https://image.tmdb.org/t/p/w500";

    // Search TMDB — no DB writes, returns lightweight results for the frontend to pick from
    public async Task<List<TmdbSearchResultDto>> SearchTmdbAsync(string query)
    {
        var results = await tmdb.SearchAsync(query);
        return results.Select(r => new TmdbSearchResultDto(
            r.Id,
            r.MediaType == "movie" ? "Movie" : "TV Show",
            r.Title ?? r.Name ?? string.Empty,
            r.OriginalTitle ?? r.OriginalName,
            r.Overview,
            r.ReleaseDate ?? r.FirstAirDate,
            r.PosterPath is not null ? $"{ImageBase}{r.PosterPath}" : null,
            r.VoteAverage
        )).ToList();
    }

    // Import a title from TMDB into DB — idempotent (update if already exists)
    public async Task<Media?> ImportFromTmdbAsync(int tmdbId, string type)
    {
        if (type.Equals("movie", StringComparison.OrdinalIgnoreCase))
        {
            var details = await tmdb.FetchMovieAsync(tmdbId);
            return details is null ? null : await UpsertMovieAsync(details);
        }
        else
        {
            var details = await tmdb.FetchTvAsync(tmdbId);
            return details is null ? null : await UpsertTvAsync(details);
        }
    }

    // Sync episodes from TVMaze for a TV show already in DB
    public async Task SyncEpisodesAsync(int mediaId, int tvMazeId)
    {
        var episodes = await tvMaze.FetchEpisodesAsync(tvMazeId);

        foreach (var ep in episodes.Where(e => e.Number.HasValue))
        {
            var existing = await db.Episodes.FirstOrDefaultAsync(e =>
                e.MediaId == mediaId &&
                e.Season == ep.Season &&
                e.NumberInSeason == ep.Number!.Value);

            if (existing is null)
            {
                db.Episodes.Add(new Episode
                {
                    MediaId = mediaId,
                    Season = ep.Season,
                    NumberInSeason = ep.Number!.Value,
                    Title = ep.Name,
                    AirDate = ParseDate(ep.AirDate),
                    Summary = StripHtml(ep.Summary)
                });
            }
            else
            {
                existing.Title = ep.Name;
                existing.AirDate = ParseDate(ep.AirDate);
                existing.Summary = StripHtml(ep.Summary);
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task<Media> UpsertMovieAsync(TmdbMovieDetails details)
    {
        var media = await db.Media.FirstOrDefaultAsync(m => m.TmdbId == details.Id);
        var movieTypeId = await db.MediaTypes.Where(t => t.Name == "Movie").Select(t => t.Id).FirstAsync();
        var genreId = await EnsureGenreAsync(details.Genres?.FirstOrDefault()?.Name);

        if (media is null)
        {
            media = new Media { TmdbId = details.Id, MediaTypeId = movieTypeId };
            db.Media.Add(media);
        }

        media.Title = details.Title;
        media.OriginalTitle = details.OriginalTitle;
        media.Plot = details.Overview;
        media.ReleaseDate = ParseDate(details.ReleaseDate);
        media.RuntimeMin = details.Runtime;
        media.Status = details.Status;
        media.GenreId = genreId;
        media.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await SyncAssetsAsync(media.Id, details.PosterPath, details.BackdropPath);
        await SyncTmdbRatingAsync(media.Id, details.VoteAverage);

        return media;
    }

    private async Task<Media> UpsertTvAsync(TmdbTvDetails details)
    {
        var media = await db.Media.FirstOrDefaultAsync(m => m.TmdbId == details.Id);
        var tvTypeId = await db.MediaTypes.Where(t => t.Name == "TV Show").Select(t => t.Id).FirstAsync();
        var genreId = await EnsureGenreAsync(details.Genres?.FirstOrDefault()?.Name);

        if (media is null)
        {
            media = new Media { TmdbId = details.Id, MediaTypeId = tvTypeId };
            db.Media.Add(media);
        }

        media.Title = details.Name;
        media.OriginalTitle = details.OriginalName;
        media.Plot = details.Overview;
        media.ReleaseDate = ParseDate(details.FirstAirDate);
        media.RuntimeMin = details.EpisodeRunTime?.FirstOrDefault();
        media.Status = details.Status;
        media.GenreId = genreId;
        media.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await SyncAssetsAsync(media.Id, details.PosterPath, details.BackdropPath);
        await SyncTmdbRatingAsync(media.Id, details.VoteAverage);

        return media;
    }

    private async Task<int?> EnsureGenreAsync(string? name)
    {
        if (name is null) return null;
        var genre = await db.Genres.FirstOrDefaultAsync(g => g.Name == name);
        if (genre is null)
        {
            genre = new Genre { Name = name };
            db.Genres.Add(genre);
            await db.SaveChangesAsync();
        }
        return genre.Id;
    }

    private async Task SyncAssetsAsync(int mediaId, string? posterPath, string? backdropPath)
    {
        var posterId = await db.AssetTypes.Where(a => a.Name == "Poster").Select(a => a.Id).FirstAsync();
        var backdropId = await db.AssetTypes.Where(a => a.Name == "Backdrop").Select(a => a.Id).FirstAsync();

        await UpsertAssetAsync(mediaId, posterId, posterPath);
        await UpsertAssetAsync(mediaId, backdropId, backdropPath);
        await db.SaveChangesAsync();
    }

    private async Task UpsertAssetAsync(int mediaId, int assetTypeId, string? path)
    {
        if (path is null) return;
        var url = $"{ImageBase}{path}";
        var exists = await db.MediaAssets.AnyAsync(a =>
            a.MediaId == mediaId && a.AssetTypeId == assetTypeId && a.Url == url);
        if (!exists)
            db.MediaAssets.Add(new MediaAsset { MediaId = mediaId, AssetTypeId = assetTypeId, Url = url });
    }

    private async Task SyncTmdbRatingAsync(int mediaId, decimal voteAverage)
    {
        if (voteAverage == 0) return;
        var sourceId = await db.RatingSources.Where(r => r.Name == "TMDB").Select(r => r.Id).FirstAsync();
        var rating = await db.Ratings.FirstOrDefaultAsync(r =>
            r.MediaId == mediaId && r.RatingSourceId == sourceId);

        if (rating is null)
            db.Ratings.Add(new Rating { MediaId = mediaId, RatingSourceId = sourceId, Score = voteAverage, RatedAt = DateTime.UtcNow });
        else
        {
            rating.Score = voteAverage;
            rating.RatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

    private static DateOnly? ParseDate(string? s) =>
        s is not null && DateOnly.TryParse(s, out var d) ? d : null;

    private static string? StripHtml(string? html) =>
        html is null ? null : Regex.Replace(html, "<.*?>", string.Empty);
}
