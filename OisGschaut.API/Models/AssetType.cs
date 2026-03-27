using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.Models;

public class AssetType
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string Name { get; set; } = string.Empty;

    public ICollection<MediaAsset> MediaAssets { get; set; } = [];
}
