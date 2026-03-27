using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.DTOs;

public record AuthResponseDto(UserDto User, string Token);

public record UpdateUserDto([MaxLength(50)] string Username);
