namespace SafeScribe.Services;

public interface ITokenBlacklistService
{
    Task AddToBlacklistAsync(string jti, DateTime expiresAt);
    Task<bool> IsBlacklistedAsync(string jti);
}