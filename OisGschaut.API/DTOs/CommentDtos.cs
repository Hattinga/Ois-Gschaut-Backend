using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.DTOs;

public record CommentDto(
    int Id,
    int UserId,
    string Username,
    string Content,
    DateTime CreatedAt
);

public record CreateCommentDto(
    int UserId,
    [MaxLength(4000)] string Content
);
