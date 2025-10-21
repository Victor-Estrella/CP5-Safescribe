using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeScribe.Dtos;
using SafeScribe.Models;
using SafeScribe.Services;

namespace SafeScribe.Controllers;

[ApiController]
[Route("api/v1/usuarios")]
[Authorize(Roles = "Admin")]
public class UsuariosController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public UsuariosController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpGet]
    public async Task<IActionResult> ObterTodosAsync()
    {
        var users = await _tokenService.GetUsersAsync();
        var data = users.Select(Mapear);
        return Ok(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorIdAsync(Guid id)
    {
        var user = await _tokenService.GetByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(Mapear(user));
    }

    private static UserResponseDto Mapear(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role
        };
    }
}
