using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly MongoDbContext _context;
    public TasksController(MongoDbContext context) => _context = context;

    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var task = new TodoTask
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            UserId = userId
        };

        await _context.Tasks.InsertOneAsync(task);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] TaskParameters param)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole("Admin");

        var filterBuilder = Builders<TodoTask>.Filter;
        var filter = filterBuilder.Empty;

        if (!isAdmin) filter &= filterBuilder.Eq(t => t.UserId, userId);

        if (!string.IsNullOrWhiteSpace(param.Search))
            filter &= filterBuilder.Regex(t => t.Title, new MongoDB.Bson.BsonRegularExpression(param.Search, "i"));

        if (!string.IsNullOrWhiteSpace(param.Status) && bool.TryParse(param.Status, out bool isCompleted))
            filter &= filterBuilder.Eq(t => t.IsCompleted, isCompleted);

        var sortBuilder = Builders<TodoTask>.Sort;
        var sort = param.SortBy?.ToLower() == "title" 
            ? sortBuilder.Ascending(t => t.Title) 
            : sortBuilder.Descending(t => t.CreatedAt);

        var totalItems = await _context.Tasks.CountDocumentsAsync(filter);
        var tasks = await _context.Tasks.Find(filter)
            .Sort(sort)
            .Skip((param.Page - 1) * param.Limit)
            .Limit(param.Limit)
            .ToListAsync();

        return Ok(new { Total = totalItems, param.Page, param.Limit, Items = tasks });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var task = await _context.Tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (task == null) return NotFound();

        if (task.UserId != Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value) && !User.IsInRole("Admin"))
            return Forbid();

        return Ok(task);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTaskDto dto)
    {
        var task = await _context.Tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (task == null) return NotFound();

        if (task.UserId != Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value) && !User.IsInRole("Admin"))
            return Forbid();

        var update = Builders<TodoTask>.Update
            .Set(t => t.Title, dto.Title)
            .Set(t => t.Description, dto.Description)
            .Set(t => t.IsCompleted, dto.IsCompleted);

        await _context.Tasks.UpdateOneAsync(t => t.Id == id, update);
        return Ok(new { message = "Задача успешно обновлена" });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var task = await _context.Tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (task == null) return NotFound();

        if (task.UserId != Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value) && !User.IsInRole("Admin"))
            return Forbid();

        await _context.Tasks.DeleteOneAsync(t => t.Id == id);
        return NoContent();
    }
}
