using SafeScribe.Dtos;
using SafeScribe.Models;

namespace SafeScribe.Services;

public interface INoteService
{
    Task<Note> CreateAsync(Guid userId, NoteCreateDto request);

    Task<Note?> GetAsync(Guid id);

    Task<Note?> UpdateAsync(Guid id, NoteUpdateDto request);

    Task<bool> DeleteAsync(Guid id);

    Task<IEnumerable<Note>> GetByUserAsync(Guid userId);
}
