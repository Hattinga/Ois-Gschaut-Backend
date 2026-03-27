using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.Models;

public class MediaType
{
    public int Id { get; set; }

    [MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Media> Media { get; set; } = [];
    public ICollection<ListItem> ListItems { get; set; } = [];
}
