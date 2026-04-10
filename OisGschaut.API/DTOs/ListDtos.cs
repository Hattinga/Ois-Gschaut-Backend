using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.DTOs;

public record ListDto(
    int Id,
    int? UserId,
    string Name,
    string? Description,
    bool IsPublic,
    int ItemCount,
    IEnumerable<string> CoverPosters,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateListDto(
    [MaxLength(100)]  string Name,
    [MaxLength(1000)] string? Description,
    bool IsPublic = false
);

public record UpdateListDto(
    [MaxLength(100)]  string? Name,
    [MaxLength(1000)] string? Description,
    bool? IsPublic
);

public record ListItemDto(
    int MediaId,
    string MediaTitle,
    string MediaType,
    string? PosterUrl,
    DateTime AddedAt,
    string? Note,
    int? SortOrder
);

public record AddListItemDto(
    int MediaId,
    [MaxLength(500)] string? Note,
    int? SortOrder
);

public record CollaboratorDto(
    int UserId,
    string Username,
    string Role,
    DateTime AddedAt
);

public record AddCollaboratorDto(
    int UserId,
    int CollaboratorRoleId
);
