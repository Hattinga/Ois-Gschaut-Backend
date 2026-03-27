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
