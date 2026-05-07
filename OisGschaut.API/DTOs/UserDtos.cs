using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.DTOs;

public record UserDto(
    int Id,
    string Email,
    string Username,
    string? OAuthProvider,
    DateTime CreatedAt
);

public record CreateUserDto(
    [MaxLength(254)] string Email,
    [MaxLength(50)]  string Username,
    [MaxLength(50)]  string? OAuthProvider,
    [MaxLength(200)] string? OAuthId
);

// For demo/pre-auth use — find-or-create by username
public record GuestLoginDto([MaxLength(50)] string Username);

public record GenreBreakdownDto(string Genre, int Count);

public record UserProfileDto(
    int Id,
    string Username,
    string? Bio,
    DateTime CreatedAt,
    int ListCount,
    int FilmsWatched,
    int EpisodesWatched,
    IEnumerable<GenreBreakdownDto> GenreBreakdown,
    IEnumerable<ListSummaryDto> Lists,
    IEnumerable<WatchedMediaDto> RecentWatched
);

public record ListSummaryDto(int Id, string Name, string? Description, bool IsPublic, int ItemCount, IEnumerable<string> CoverPosters);

// Public-safe user info (no email) — used for search results
public record UserPublicDto(int Id, string Username);

public record WatchedMediaDto(int MediaId, string Title, string? PosterUrl, int Season, DateTime WatchedAt);
