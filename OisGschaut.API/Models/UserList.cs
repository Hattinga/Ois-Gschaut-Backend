using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.Models;

public class UserList
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsPublic { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<ListCollaborator> Collaborators { get; set; } = [];
    public ICollection<ListItem> Items { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
}
