namespace OisGschaut.API.DTOs;

// Response: one watched entry
public record WatchedDto(int UserId, int MediaId, int Season, DateTime WatchedAt);

// Request: mark a season (or whole movie, Season=0) as watched
public record MarkWatchedDto(int MediaId, int Season);

// Watchlist toggle
public record WatchlistToggleRequestDto(int MediaId);
public record WatchlistToggleDto(int MediaId, bool Saved);
