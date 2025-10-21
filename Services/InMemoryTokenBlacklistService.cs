using System.Collections.Concurrent;

namespace SafeScribe.Services;

public class InMemoryTokenBlacklistService : ITokenBlacklistService
{
    private readonly ConcurrentDictionary<string, DateTime> _blacklist = new();

    public Task AddToBlacklistAsync(string jti, DateTime expiresAt)
    {
        _blacklist[jti] = expiresAt;
        return Task.CompletedTask;
    }

    public Task<bool> IsBlacklistedAsync(string jti)
    {
        if (_blacklist.TryGetValue(jti, out var exp))
        {
            if (DateTime.UtcNow < exp)
                return Task.FromResult(true);
            _blacklist.TryRemove(jti, out _); // limpa expirados
        }
        return Task.FromResult(false);
    }
}