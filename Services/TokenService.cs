using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SafeScribe.Dtos;
using SafeScribe.Models;

namespace SafeScribe.Services;

/// <summary>
/// Serviço responsável por autenticação e geração de tokens JWT.
/// </summary>
public class TokenService : ITokenService
{
    private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);
    private readonly IConfiguration _configuration;
    // Blacklist de tokens inválidos (logout)
    private readonly ConcurrentDictionary<string, DateTime> _tokenBlacklist = new();
    /// <summary>
    /// Invalida um token JWT (logout).
    /// </summary>
    public Task LogoutAsync(string token)
    {
        // Adiciona o token à blacklist até sua expiração
        var handler = new JwtSecurityTokenHandler();
        if (handler.CanReadToken(token))
        {
            var jwt = handler.ReadJwtToken(token);
            var exp = jwt.ValidTo;
            _tokenBlacklist[token] = exp;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifica se o token está na blacklist.
    /// </summary>
    public bool IsTokenBlacklisted(string token)
    {
        if (_tokenBlacklist.TryGetValue(token, out var exp))
        {
            if (DateTime.UtcNow < exp)
                return true;
            // Remove tokens expirados
            _tokenBlacklist.TryRemove(token, out _);
        }
        return false;
    }

    /// <summary>
    /// Injeta dependência de configuração.
    /// </summary>
    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Registra um novo usuário em memória.
    /// </summary>
    public Task<User> RegisterAsync(UserRegisterDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ArgumentException("O nome de utilizador é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("A senha é obrigatória.");
        }

        if (_users.ContainsKey(request.Username))
        {
            throw new InvalidOperationException("O nome de utilizador já está registado.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role
        };

        if (!_users.TryAdd(user.Username, user))
        {
            throw new InvalidOperationException("Não foi possível registar o utilizador.");
        }

        return Task.FromResult(user);
    }

    /// <summary>
    /// Autentica um usuário e retorna um token JWT.
    /// </summary>
    public Task<string> AuthenticateAsync(LoginRequestDto request)
    {
        if (!_users.TryGetValue(request.Username, out var user))
        {
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        var token = CreateToken(user);
        return Task.FromResult(token);
    }

    /// <summary>
    /// Cria um token JWT para o usuário autenticado.
    /// </summary>
    private string CreateToken(User user)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Chave JWT não configurada.");
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        // Reivindicações descrevem a identidade do utilizador autenticado.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}