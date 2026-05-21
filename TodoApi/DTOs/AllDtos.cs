namespace TodoApi.DTOs;

public record RegisterDto(string Email, string Password);
public record LoginDto(string Email, string Password);
public record TokenDto(string AccessToken, string RefreshToken);
public record UpdateUserDto(string Email);
public record CreateTaskDto(string Title, string? Description);
public record UpdateTaskDto(string Title, string? Description, bool IsCompleted);
public record TaskResponseDto(Guid Id, string Title, string? Description, bool IsCompleted, DateTime CreatedAt, Guid UserId);
public record TaskParameters(string? Status, string? Search, int Page = 1, int Limit = 10, string? SortBy = "Date");