using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.Models;

public class ListItem
{
    public int ListId { get; set; }
    public int MediaId { get; set; }
    public int MediaTypeId { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Note { get; set; }

    public int? SortOrder { get; set; }

    public UserList List { get; set; } = null!;
    public Media Media { get; set; } = null!;
    public MediaType MediaType { get; set; } = null!;
}
