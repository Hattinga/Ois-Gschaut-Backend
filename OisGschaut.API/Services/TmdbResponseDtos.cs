using System.Text.Json.Serialization;

namespace OisGschaut.API.Services;

public record TmdbSearchResponse(
    [property: JsonPropertyName("results")] List<TmdbSearchItem> Results
);

public record TmdbSearchItem(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("media_type")] string MediaType,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("original_title")] string? OriginalTitle,
    [property: JsonPropertyName("original_name")] string? OriginalName,
    [property: JsonPropertyName("overview")] string? Overview,
    [property: JsonPropertyName("release_date")] string? ReleaseDate,
    [property: JsonPropertyName("first_air_date")] string? FirstAirDate,
    [property: JsonPropertyName("poster_path")] string? PosterPath,
    [property: JsonPropertyName("backdrop_path")] string? BackdropPath,
    [property: JsonPropertyName("vote_average")] decimal VoteAverage,
    [property: JsonPropertyName("genre_ids")] List<int>? GenreIds
);

public record TmdbMovieDetails(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("original_title")] string? OriginalTitle,
    [property: JsonPropertyName("overview")] string? Overview,
    [property: JsonPropertyName("release_date")] string? ReleaseDate,
    [property: JsonPropertyName("runtime")] int? Runtime,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("poster_path")] string? PosterPath,
    [property: JsonPropertyName("backdrop_path")] string? BackdropPath,
    [property: JsonPropertyName("genres")] List<TmdbGenre>? Genres,
    [property: JsonPropertyName("vote_average")] decimal VoteAverage
);

public record TmdbTvDetails(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("original_name")] string? OriginalName,
    [property: JsonPropertyName("overview")] string? Overview,
    [property: JsonPropertyName("first_air_date")] string? FirstAirDate,
    [property: JsonPropertyName("episode_run_time")] List<int>? EpisodeRunTime,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("poster_path")] string? PosterPath,
    [property: JsonPropertyName("backdrop_path")] string? BackdropPath,
    [property: JsonPropertyName("genres")] List<TmdbGenre>? Genres,
    [property: JsonPropertyName("vote_average")] decimal VoteAverage
);

public record TmdbGenre(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name
);
