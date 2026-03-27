using System.ComponentModel.DataAnnotations;

namespace OisGschaut.API.Models;

public class CollaboratorRole
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<ListCollaborator> ListCollaborators { get; set; } = [];
}
