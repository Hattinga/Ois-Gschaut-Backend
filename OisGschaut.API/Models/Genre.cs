using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.Models;

public class Genre
{
    public int Id { get; set; }

    [MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Media> Media { get; set; } = [];
}
