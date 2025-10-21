using SafeScribe.Models;

namespace SafeScribe.Dtos;

public class UserResponseDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public UserRole Role { get; set; }
}
