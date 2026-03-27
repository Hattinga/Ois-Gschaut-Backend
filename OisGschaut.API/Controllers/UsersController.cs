using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OisGschaut.API.Data;
using OisGschaut.API.DTOs;
using OisGschaut.API.Models;

namespace OisGschaut.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await db.Users
            .Select(u => new UserDto(u.Id, u.Email, u.Username, u.OAuthProvider, u.CreatedAt))
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        return Ok(new UserDto(user.Id, user.Email, user.Username, user.OAuthProvider, user.CreatedAt));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserDto dto)
    {
        var user = new User
        {
            Email         = dto.Email,
            Username      = dto.Username,
            OAuthProvider = dto.OAuthProvider,
            OAuthId       = dto.OAuthId
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var result = new UserDto(user.Id, user.Email, user.Username, user.OAuthProvider, user.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
