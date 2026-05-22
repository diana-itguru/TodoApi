using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly MongoDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthController(MongoDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var existingUser = await _context.Users.Find(u => u.Email == dto.Email).FirstOrDefaultAsync();
        if (existingUser != null) return BadRequest(new { message = "Email уже занят" });

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Email.Contains("admin") ? UserRole.Admin : UserRole.User // Авто-назначение админа по подстроке для тестов
        };

        await _context.Users.InsertOneAsync(user);
        return Ok(new { message = "Регистрация успешна" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users.Find(u => u.Email == dto.Email).FirstOrDefaultAsync();
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Неверный email или пароль" });

        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var accessToken = _tokenService.GenerateAccessToken(claims);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var update = Builders<User>.Update
            .Set(u => u.RefreshToken, refreshToken)
            .Set(u => u.RefreshTokenExpiryTime, DateTime.UtcNow.AddDays(7));

        await _context.Users.UpdateOneAsync(u => u.Id == user.Id, update);

        return Ok(new TokenDto(accessToken, refreshToken));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(TokenDto dto)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
        if (principal == null) return BadRequest(new { message = "Невалидный access токен" });

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync(); 

        if (user == null || user.RefreshToken != dto.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return BadRequest(new { message = "Невалидный или истекший refresh токен" });

        var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        var update = Builders<User>.Update.Set(u => u.RefreshToken, newRefreshToken);
        await _context.Users.UpdateOneAsync(u => u.Id == user.Id, update);

        return Ok(new TokenDto(newAccessToken, newRefreshToken));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var update = Builders<User>.Update
            .Set(u => u.RefreshToken, null)
            .Set(u => u.RefreshTokenExpiryTime, null);
            
        await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        return Ok(new { message = "Выход успешен" });
    }
}
