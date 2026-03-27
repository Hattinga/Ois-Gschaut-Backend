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
    // ── Lists ─────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ListDto>>> GetAll()
    {
        var lists = await db.Lists
            .Select(l => new ListDto(l.Id, l.UserId, l.Name, l.Description, l.IsPublic, l.CreatedAt, l.UpdatedAt))
            .ToListAsync();
        return Ok(lists);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ListDto>> GetById(int id)
    {
        var list = await db.Lists.FindAsync(id);
        if (list is null) return NotFound();
        return Ok(new ListDto(list.Id, list.UserId, list.Name, list.Description, list.IsPublic, list.CreatedAt, list.UpdatedAt));
    }

    [HttpPost]
    public async Task<ActionResult<ListDto>> Create(int userId, CreateListDto dto)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId))
            return NotFound("User not found.");

        var list = new UserList
        {
            UserId      = userId,
            Name        = dto.Name,
            Description = dto.Description,
            IsPublic    = dto.IsPublic
        };
        db.Lists.Add(list);
        await db.SaveChangesAsync();
        var result = new ListDto(list.Id, list.UserId, list.Name, list.Description, list.IsPublic, list.CreatedAt, list.UpdatedAt);
        return CreatedAtAction(nameof(GetById), new { id = list.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ListDto>> Update(int id, UpdateListDto dto)
    {
        var list = await db.Lists.FindAsync(id);
        if (list is null) return NotFound();

        if (dto.Name        is not null) list.Name        = dto.Name;
        if (dto.Description is not null) list.Description = dto.Description;
        if (dto.IsPublic    is not null) list.IsPublic    = dto.IsPublic.Value;
        list.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(new ListDto(list.Id, list.UserId, list.Name, list.Description, list.IsPublic, list.CreatedAt, list.UpdatedAt));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var list = await db.Lists.FindAsync(id);
        if (list is null) return NotFound();
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
            .Include(li => li.Media)
            .Include(li => li.MediaType)
            .Select(li => new ListItemDto(
                li.MediaId,
                li.Media.Title,
                li.MediaType.Name,
                li.AddedAt,
                li.Note,
                li.SortOrder))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("{id:int}/items")]
    public async Task<IActionResult> AddItem(int id, AddListItemDto dto)
    {
        if (!await db.Lists.AnyAsync(l => l.Id == id)) return NotFound("List not found.");
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

    [HttpDelete("{id:int}/items/{mediaId:int}")]
    public async Task<IActionResult> RemoveItem(int id, int mediaId)
    {
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

    [HttpPost("{id:int}/collaborators")]
    public async Task<IActionResult> AddCollaborator(int id, AddCollaboratorDto dto)
    {
        if (!await db.Lists.AnyAsync(l => l.Id == id)) return NotFound("List not found.");
        if (!await db.Users.AnyAsync(u => u.Id == dto.UserId)) return NotFound("User not found.");
        if (!await db.CollaboratorRoles.AnyAsync(r => r.Id == dto.CollaboratorRoleId)) return BadRequest("Invalid role.");

        if (await db.ListCollaborators.AnyAsync(lc => lc.ListId == id && lc.UserId == dto.UserId))
            return Conflict("User is already a collaborator.");

        db.ListCollaborators.Add(new ListCollaborator
        {
            ListId              = id,
            UserId              = dto.UserId,
            CollaboratorRoleId  = dto.CollaboratorRoleId
        });
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}/collaborators/{userId:int}")]
    public async Task<IActionResult> RemoveCollaborator(int id, int userId)
    {
        var collab = await db.ListCollaborators.FindAsync(id, userId);
        if (collab is null) return NotFound();
        db.ListCollaborators.Remove(collab);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
