using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeScribe.Dtos;
using SafeScribe.Models;
using SafeScribe.Services;


/// <summary>
/// Controller responsável pela gestão de notas.
/// </summary>
namespace SafeScribe.Controllers;

[ApiController]
[Route("api/v1/notas")]
[Authorize]
/// <summary>
/// Endpoints para CRUD de notas.
/// </summary>
public class NotasController : ControllerBase
{
    private readonly INoteService _noteService;

    /// <summary>
    /// Injeta dependência do serviço de notas.
    /// </summary>
    public NotasController(INoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpPost]
    [Authorize(Roles = "Editor,Admin")]
    /// <summary>
    /// Cria uma nova nota para o usuário autenticado.
    /// </summary>
    public async Task<IActionResult> CriarAsync([FromBody] NoteCreateDto request)
    {
        var userId = ObterIdUtilizador();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { mensagem = "Utilizador inválido." });
        }

        var note = await _noteService.CreateAsync(userId, request);
        // Usa rota nomeada para evitar falhas na geração de URL
        return CreatedAtRoute("GetNotaById", new { id = note.Id }, Mapear(note));
    }

    [HttpGet("{id:guid}", Name = "GetNotaById")]
    /// <summary>
    /// Obtém uma nota pelo ID, respeitando permissões do usuário.
    /// </summary>
    public async Task<IActionResult> ObterPorIdAsync(Guid id)
    {
        var note = await _noteService.GetAsync(id);
        if (note is null)
        {
            return NotFound();
        }

        var userId = ObterIdUtilizador();
        var role = ObterRole();
        if (role != UserRole.Admin && note.UserId != userId)
        {
            return Forbid();
        }

        return Ok(Mapear(note));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Editor,Admin")]
    /// <summary>
    /// Atualiza uma nota existente, se o usuário for dono ou admin.
    /// </summary>
    public async Task<IActionResult> AtualizarAsync(Guid id, [FromBody] NoteUpdateDto request)
    {
        var note = await _noteService.GetAsync(id);
        if (note is null)
        {
            return NotFound();
        }

        var userId = ObterIdUtilizador();
        var role = ObterRole();
        if (role != UserRole.Admin && note.UserId != userId)
        {
            return Forbid();
        }

        note = await _noteService.UpdateAsync(id, request);
        return Ok(Mapear(note!));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    /// <summary>
    /// Remove uma nota, apenas para administradores.
    /// </summary>
    public async Task<IActionResult> ApagarAsync(Guid id)
    {
        var apagou = await _noteService.DeleteAsync(id);
        if (!apagou)
        {
            return NotFound();
        }

        return NoContent();
    }

    private Guid ObterIdUtilizador()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private UserRole ObterRole()
    {
        var claim = User.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<UserRole>(claim, out var role) ? role : UserRole.Leitor;
    }

    private static NoteResponseDto Mapear(Note note)
    {
        return new NoteResponseDto
        {
            Id = note.Id,
            Title = note.Title,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            UserId = note.UserId
        };
    }
}