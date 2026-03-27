using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.Models;

public class RatingSource
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Rating> Ratings { get; set; } = [];
}
