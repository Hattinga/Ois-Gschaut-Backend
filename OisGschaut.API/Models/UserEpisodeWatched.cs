namespace OisGschaut.API.Models;

public class UserEpisodeWatched
{
    public int UserId { get; set; }
    public int EpisodeId { get; set; }
    public DateTime WatchedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Episode Episode { get; set; } = null!;
}
