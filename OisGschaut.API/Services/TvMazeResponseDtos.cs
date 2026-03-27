using System.Text.Json.Serialization;

namespace OisGschaut.API.Services;

public record TvMazeEpisode(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("season")] int Season,
    [property: JsonPropertyName("number")] int? Number,
    [property: JsonPropertyName("airdate")] string? AirDate,
    [property: JsonPropertyName("summary")] string? Summary
);

public record TvMazeShowResult(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string? Name
);
