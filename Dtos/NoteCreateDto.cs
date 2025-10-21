namespace SafeScribe.Dtos;

public class NoteCreateDto
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    // Permite que administradores atribuam a nota a outro utilizador opcionalmente.
    public Guid? UserId { get; set; }
}
