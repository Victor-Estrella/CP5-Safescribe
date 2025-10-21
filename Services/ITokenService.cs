using SafeScribe.Dtos;
using SafeScribe.Models;

namespace SafeScribe.Services;

public interface ITokenService
{
    Task<User> RegisterAsync(UserRegisterDto request);

    Task<string> AuthenticateAsync(LoginRequestDto request);

    /// <summary>
    /// Invalida um token JWT (logout).
    /// </summary>
    Task LogoutAsync(string token);

    /// <summary>
    /// Verifica se o token está inválido (blacklist).
    /// </summary>
    bool IsTokenBlacklisted(string token);
}