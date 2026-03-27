using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.Models;

public class MediaAsset
{
    public int Id { get; set; }

    public int MediaId { get; set; }
    public int AssetTypeId { get; set; }

    [MaxLength(2048)]
    public string Url { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Media Media { get; set; } = null!;
    public AssetType AssetType { get; set; } = null!;
}
