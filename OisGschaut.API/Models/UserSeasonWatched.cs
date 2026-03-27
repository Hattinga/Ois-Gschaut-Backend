namespace OisGschaut.API.Models;

public class UserSeasonWatched
{
    public int UserId { get; set; }
    public int MediaId { get; set; }
    public int Season { get; set; }  // 0 = whole movie / special "watched" marker

    public DateTime WatchedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Media Media { get; set; } = null!;
}
