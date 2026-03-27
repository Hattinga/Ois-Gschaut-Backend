namespace OisGschaut.API.Models;

public class Rating
{
    public int Id { get; set; }

    public int MediaId { get; set; }
    public int RatingSourceId { get; set; }

    public decimal Score { get; set; }  // 0–10, precision (4,2)

    public DateTime RatedAt { get; set; } = DateTime.UtcNow;

    public Media Media { get; set; } = null!;
    public RatingSource RatingSource { get; set; } = null!;
}
