using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.Models;

public class User
{
    public int Id { get; set; }

    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? OAuthProvider { get; set; }

    [MaxLength(200)]
    public string? OAuthId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserList> Lists { get; set; } = [];
    public ICollection<ListCollaborator> Collaborations { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<UserSeasonWatched> WatchedSeasons { get; set; } = [];
}
