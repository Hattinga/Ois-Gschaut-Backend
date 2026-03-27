using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OisGschaut.API.Data;
using OisGschaut.API.DTOs;
using OisGschaut.API.Models;

namespace OisGschaut.API.Controllers;

[ApiController]
[Route("api/lists/{listId:int}/comments")]
public class CommentsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetByList(int listId)
    {
        if (!await db.Lists.AnyAsync(l => l.Id == listId)) return NotFound();

        var comments = await db.Comments
            .Where(c => c.ListId == listId)
            .Include(c => c.User)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(c.Id, c.UserId, c.User.Username, c.Content, c.CreatedAt))
            .ToListAsync();

        return Ok(comments);
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create(int listId, CreateCommentDto dto)
    {
        if (!await db.Lists.AnyAsync(l => l.Id == listId)) return NotFound("List not found.");
        if (!await db.Users.AnyAsync(u => u.Id == dto.UserId)) return NotFound("User not found.");

        var comment = new Comment
        {
            ListId  = listId,
            UserId  = dto.UserId,
            Content = dto.Content
        };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        var user = await db.Users.FindAsync(dto.UserId);
        return CreatedAtAction(nameof(GetByList), new { listId },
            new CommentDto(comment.Id, comment.UserId, user!.Username, comment.Content, comment.CreatedAt));
    }
}
