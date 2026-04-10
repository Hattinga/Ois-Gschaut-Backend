using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OisGschaut.API.Data;
using OisGschaut.API.DTOs;
using OisGschaut.API.Models;

namespace OisGschaut.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListsController(AppDbContext db) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Lists ─────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ListDto>>> GetAll([FromQuery] int? userId)
    {
        var query = db.Lists.AsQueryable();
        if (userId.HasValue) query = query.Where(l => l.UserId == userId.Value);

        var lists = await query
            .Select(l => new ListDto(
                l.Id, l.UserId, l.Name, l.Description, l.IsPublic,
                l.Items.Count,
                l.CreatedAt, l.UpdatedAt))
            .ToListAsync();
        return Ok(lists);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ListDto>> GetById(int id)
    {
        var list = await db.Lists
            .Where(l => l.Id == id)
            .Select(l => new ListDto(
                l.Id, l.UserId, l.Name, l.Description, l.IsPublic,
                l.Items.Count,
                l.CreatedAt, l.UpdatedAt))
            .FirstOrDefaultAsync();

        if (list is null) return NotFound();
        return Ok(list);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ListDto>> Create([FromBody] CreateListDto dto)
    {
        var list = new UserList
        {
            UserId      = CurrentUserId,
            Name        = dto.Name,
            Description = dto.Description,
            IsPublic    = dto.IsPublic
        };
        db.Lists.Add(list);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = list.Id },
            new ListDto(list.Id, list.UserId, list.Name, list.Description, list.IsPublic, 0, list.CreatedAt, list.UpdatedAt));
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ListDto>> Update(int id, UpdateListDto dto)
    {
        var list = await db.Lists.FindAsync(id);
        if (list is null) return NotFound();
        if (list.UserId != CurrentUserId) return Forbid();

        if (dto.Name        is not null) list.Name        = dto.Name;
        if (dto.Description is not null) list.Description = dto.Description;
        if (dto.IsPublic    is not null) list.IsPublic    = dto.IsPublic.Value;
        list.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var itemCount = await db.ListItems.CountAsync(li => li.ListId == id);
        return Ok(new ListDto(list.Id, list.UserId, list.Name, list.Description, list.IsPublic, itemCount, list.CreatedAt, list.UpdatedAt));
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var list = await db.Lists.FindAsync(id);
        if (list is null) return NotFound();
        if (list.UserId != CurrentUserId) return Forbid();

        db.Lists.Remove(list);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Items ─────────────────────────────────────────────────────────────

    [HttpGet("{id:int}/items")]
    public async Task<ActionResult<IEnumerable<ListItemDto>>> GetItems(int id)
    {
        if (!await db.Lists.AnyAsync(l => l.Id == id)) return NotFound();

        var items = await db.ListItems
            .Where(li => li.ListId == id)
            .Include(li => li.Media).ThenInclude(m => m.Assets).ThenInclude(a => a.AssetType)
            .Include(li => li.MediaType)
            .OrderBy(li => li.SortOrder).ThenBy(li => li.AddedAt)
            .Select(li => new ListItemDto(
                li.MediaId,
                li.Media.Title,
                li.MediaType.Name,
                li.Media.Assets
                    .Where(a => a.AssetType.Name == "Poster")
                    .Select(a => a.Url)
                    .FirstOrDefault(),
                li.AddedAt,
                li.Note,
                li.SortOrder))
            .ToListAsync();

        return Ok(items);
    }

    [Authorize]
    [HttpPost("{id:int}/items")]
    public async Task<IActionResult> AddItem(int id, AddListItemDto dto)
    {
        if (!await db.Lists.AnyAsync(l => l.Id == id)) return NotFound("List not found.");
        if (!await CanEditListAsync(id)) return Forbid();

        var media = await db.Media.FindAsync(dto.MediaId);
        if (media is null) return NotFound("Media not found.");

        if (await db.ListItems.AnyAsync(li => li.ListId == id && li.MediaId == dto.MediaId))
            return Conflict("Media already in list.");

        db.ListItems.Add(new ListItem
        {
            ListId      = id,
            MediaId     = dto.MediaId,
            MediaTypeId = media.MediaTypeId,
            Note        = dto.Note,
            SortOrder   = dto.SortOrder
        });
        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id:int}/items/{mediaId:int}")]
    public async Task<IActionResult> RemoveItem(int id, int mediaId)
    {
        if (!await CanEditListAsync(id)) return Forbid();

        var item = await db.ListItems.FindAsync(id, mediaId);
        if (item is null) return NotFound();
        db.ListItems.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Collaborators ─────────────────────────────────────────────────────

    [HttpGet("{id:int}/collaborators")]
    public async Task<ActionResult<IEnumerable<CollaboratorDto>>> GetCollaborators(int id)
    {
        if (!await db.Lists.AnyAsync(l => l.Id == id)) return NotFound();

        var collaborators = await db.ListCollaborators
            .Where(lc => lc.ListId == id)
            .Include(lc => lc.User)
            .Include(lc => lc.CollaboratorRole)
            .Select(lc => new CollaboratorDto(lc.UserId, lc.User.Username, lc.CollaboratorRole.Name, lc.AddedAt))
            .ToListAsync();

        return Ok(collaborators);
    }

    [Authorize]
    [HttpPost("{id:int}/collaborators")]
    public async Task<IActionResult> AddCollaborator(int id, AddCollaboratorDto dto)
    {
        if (!await db.Lists.AnyAsync(l => l.Id == id)) return NotFound("List not found.");
        if (!await IsOwnerOrAdminAsync(id)) return Forbid();
        if (!await db.Users.AnyAsync(u => u.Id == dto.UserId)) return NotFound("User not found.");
        if (!await db.CollaboratorRoles.AnyAsync(r => r.Id == dto.CollaboratorRoleId)) return BadRequest("Invalid role.");

        if (await db.ListCollaborators.AnyAsync(lc => lc.ListId == id && lc.UserId == dto.UserId))
            return Conflict("User is already a collaborator.");

        db.ListCollaborators.Add(new ListCollaborator
        {
            ListId             = id,
            UserId             = dto.UserId,
            CollaboratorRoleId = dto.CollaboratorRoleId
        });
        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id:int}/collaborators/{userId:int}")]
    public async Task<IActionResult> RemoveCollaborator(int id, int userId)
    {
        var list = await db.Lists.FindAsync(id);
        if (list is null) return NotFound();
        if (list.UserId != CurrentUserId) return Forbid();

        var collab = await db.ListCollaborators.FindAsync(id, userId);
        if (collab is null) return NotFound();
        db.ListCollaborators.Remove(collab);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    // Owner or Editor (role ≤ 3) can add/remove items
    private async Task<bool> CanEditListAsync(int listId)
    {
        var list = await db.Lists.FindAsync(listId);
        if (list is null) return false;
        if (list.UserId == CurrentUserId) return true;
        return await db.ListCollaborators.AnyAsync(lc =>
            lc.ListId == listId && lc.UserId == CurrentUserId && lc.CollaboratorRoleId <= 3);
    }

    // Owner or Admin (role ≤ 2) can manage collaborators
    private async Task<bool> IsOwnerOrAdminAsync(int listId)
    {
        var list = await db.Lists.FindAsync(listId);
        if (list is null) return false;
        if (list.UserId == CurrentUserId) return true;
        return await db.ListCollaborators.AnyAsync(lc =>
            lc.ListId == listId && lc.UserId == CurrentUserId && lc.CollaboratorRoleId <= 2);
    }
}
