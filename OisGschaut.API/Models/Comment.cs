using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.Models;

public class Comment
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public int ListId { get; set; }

    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public UserList List { get; set; } = null!;
}
