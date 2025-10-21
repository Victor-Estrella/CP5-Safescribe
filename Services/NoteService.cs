using System.Collections.Concurrent;
using SafeScribe.Dtos;
using SafeScribe.Models;

namespace SafeScribe.Services;

/// <summary>
/// Servi�o respons�vel pela gest�o de notas em mem�ria.
/// </summary>
public class NoteService : INoteService
{
    private readonly ConcurrentDictionary<Guid, Note> _notes = new();

    /// <summary>
    /// Cria uma nova nota para o usu�rio.
    /// </summary>
    public Task<Note> CreateAsync(Guid userId, NoteCreateDto request)
    {
        var note = new Note
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        _notes[note.Id] = note;
        return Task.FromResult(note);
    }

    /// <summary>
    /// Obt�m uma nota pelo ID.
    /// </summary>
    public Task<Note?> GetAsync(Guid id)
    {
        _notes.TryGetValue(id, out var note);
        return Task.FromResult(note);
    }

    /// <summary>
    /// Atualiza uma nota existente.
    /// </summary>
    public Task<Note?> UpdateAsync(Guid id, NoteUpdateDto request)
    {
        if (!_notes.TryGetValue(id, out var existing))
        {
            return Task.FromResult<Note?>(null);
        }

        existing.Title = request.Title;
        existing.Content = request.Content;

        return Task.FromResult<Note?>(existing);
    }

    /// <summary>
    /// Remove uma nota pelo ID.
    /// </summary>
    public Task<bool> DeleteAsync(Guid id)
    {
        return Task.FromResult(_notes.TryRemove(id, out _));
    }

    /// <summary>
    /// Obt�m todas as notas de um usu�rio.
    /// </summary>
    public Task<IEnumerable<Note>> GetByUserAsync(Guid userId)
    {
        var result = _notes.Values.Where(note => note.UserId == userId);
        return Task.FromResult(result);
    }
}