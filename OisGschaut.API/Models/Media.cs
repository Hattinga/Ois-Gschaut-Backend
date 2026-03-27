using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.Models;

public class Media
{
    public int Id { get; set; }

    public int? TmdbId { get; set; }
    public int? TvMazeId { get; set; }

    public int MediaTypeId { get; set; }
    public int? GenreId { get; set; }

    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? OriginalTitle { get; set; }

    public string? Plot { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    [MaxLength(30)]
    public string? Status { get; set; }

    public int? RuntimeMin { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public MediaType MediaType { get; set; } = null!;
    public Genre? Genre { get; set; }

    public ICollection<ListItem> ListItems { get; set; } = [];
    public ICollection<Episode> Episodes { get; set; } = [];
    public ICollection<Rating> Ratings { get; set; } = [];
    public ICollection<MediaAsset> Assets { get; set; } = [];
}
