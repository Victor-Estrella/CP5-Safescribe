namespace SafeScribe.Dtos;

public class NoteResponseDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Guid UserId { get; set; }
}
