using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeScribe.Dtos;
using SafeScribe.Services;

/// <summary>
/// Controller responsável pela autenticação e registro de usuários.
/// </summary>
namespace SafeScribe.Controllers;

[ApiController]
[Route("api/v1/auth")]
/// <summary>
/// Endpoints de autenticação e registro.
/// </summary>
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Injeta dependência do serviço de token.
    /// </summary>
    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    /// <summary>
    /// Registra um novo usuário.
    /// </summary>
    [HttpPost("registrar")]
    [AllowAnonymous]
    public async Task<IActionResult> RegistrarAsync([FromBody] UserRegisterDto request)
    {
        try
        {
            var user = await _tokenService.RegisterAsync(request);
            // Retorna 201 Created com URL explícita para o endpoint de login, evitando geração de rota
            return Created("/api/v1/auth/login", new { user.Id, user.Username, Role = user.Role.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>
    /// Realiza login e retorna um token JWT.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDto request)
    {
        try
        {
            var token = await _tokenService.AuthenticateAsync(request);
            return Ok(new { token });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { mensagem = ex.Message });
        }
    }

    /// <summary>
    /// Realiza logout do usuário (invalida o token JWT).
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync([FromServices] Services.ITokenBlacklistService blacklistService)
    {
        var jti = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
        var expClaim = User.FindFirst("exp")?.Value;
        if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(expClaim))
            return BadRequest(new { mensagem = "Token inválido." });

        // exp é em segundos desde epoch
        var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime;
        await blacklistService.AddToBlacklistAsync(jti, exp);
        return Ok(new { mensagem = "Logout realizado com sucesso." });
    }
}
