using MongoDB.Bson.Serialization.Attributes;

namespace TodoApi.Models;

public enum UserRole { User, Admin }

public class User
{
    [BsonId]
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.User;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
}