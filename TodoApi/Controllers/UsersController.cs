using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly MongoDbContext _context;
    public UsersController(MongoDbContext context) => _context = context;

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _context.Users.Find(u => u.Id == userId).Project(u => new { u.Id, u.Email, u.Role }).FirstOrDefaultAsync();
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateUserDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        
        var existingEmail = await _context.Users.Find(u => u.Email == dto.Email && u.Id != userId).FirstOrDefaultAsync();
        if (existingEmail != null) return BadRequest(new { message = "Этот email уже используется" });

        var update = Builders<User>.Update.Set(u => u.Email, dto.Email);
        var result = await _context.Users.UpdateOneAsync(u => u.Id == userId, update);

        return result.ModifiedCount == 0 ? NotFound() : Ok(new { message = "Данные обновлены" });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users.Find(_ => true).Project(u => new { u.Id, u.Email, u.Role }).ToListAsync();
        return Ok(users);
    }
}