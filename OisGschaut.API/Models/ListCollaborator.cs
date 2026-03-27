namespace OisGschaut.API.Models;

public class ListCollaborator
{
    public int ListId { get; set; }
    public int UserId { get; set; }
    public int CollaboratorRoleId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public int? InvitedByUserId { get; set; }

    public UserList List { get; set; } = null!;
    public User User { get; set; } = null!;
    public CollaboratorRole CollaboratorRole { get; set; } = null!;
}
