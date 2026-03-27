namespace OisGschaut.API.DTOs;

// Response: one watched entry
public record WatchedDto(int UserId, int MediaId, int Season, DateTime WatchedAt);

// Request: mark a season (or whole movie, Season=0) as watched
public record MarkWatchedDto(int UserId, int MediaId, int Season);
