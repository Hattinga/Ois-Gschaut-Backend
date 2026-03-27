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

public record UserProfileDto(
    int Id,
    string Username,
    DateTime CreatedAt,
    int ListCount,
    int FilmsWatched,
    IEnumerable<ListSummaryDto> Lists,
    IEnumerable<WatchedMediaDto> RecentWatched
);

public record ListSummaryDto(int Id, string Name, string? Description, bool IsPublic, int ItemCount);

public record WatchedMediaDto(int MediaId, string Title, string? PosterUrl, int Season, DateTime WatchedAt);
