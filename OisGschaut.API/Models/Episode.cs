using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.Models;

public class Episode
{
    public int Id { get; set; }

    public int MediaId { get; set; }
    public int Season { get; set; }
    public int NumberInSeason { get; set; }

    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    public DateOnly? AirDate { get; set; }
    public string? Summary { get; set; }

    public Media Media { get; set; } = null!;
}
