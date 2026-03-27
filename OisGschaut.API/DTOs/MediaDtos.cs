namespace OisGschaut.API.DTOs;

public record MediaDto(
    int Id,
    int? TmdbId,
    int? TvMazeId,
    string Title,
    string? OriginalTitle,
    string MediaType,
    string? Genre,
    string? Plot,
    DateOnly? ReleaseDate,
    string? Status,
    int? RuntimeMin,
    IEnumerable<RatingDto> Ratings,
    IEnumerable<MediaAssetDto> Assets
);

public record RatingDto(
    string Source,
    decimal Score
);

public record MediaAssetDto(
    string AssetType,
    string Url
);

public record EpisodeDto(
    int Id,
    int Season,
    int NumberInSeason,
    string Title,
    DateOnly? AirDate,
    string? Summary
);

// TMDB search result — not persisted, used to let the user pick what to import
public record TmdbSearchResultDto(
    int TmdbId,
    string MediaType,
    string Title,
    string? OriginalTitle,
    string? Overview,
    string? ReleaseDate,
    string? PosterUrl,
    decimal VoteAverage
);

// Request body for POST /api/media/import
public record ImportMediaDto(
    int TmdbId,
    string Type  // "movie" | "tv"
);
