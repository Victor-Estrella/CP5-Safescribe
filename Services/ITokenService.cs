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
    /// Verifica se o token est� inv�lido (blacklist).
    /// </summary>
    bool IsTokenBlacklisted(string token);

    /// <summary>
    /// Recupera todos os utilizadores conhecidos pelo sistema.
    /// </summary>
    Task<IEnumerable<User>> GetUsersAsync();

    /// <summary>
    /// Procura um utilizador pelo identificador.
    /// </summary>
    Task<User?> GetByIdAsync(Guid id);
}