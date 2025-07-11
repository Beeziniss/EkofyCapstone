
namespace EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
public interface IRedisCacheService
{
    Task SetAsync(string key, string value, TimeSpan? expiry = null);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<string?> GetAsync(string key);
    bool TryGet(string key, out string? value);
    Task<ICacheResult<string>> TryGetAsync(string key);
    bool TryGet<T>(string key, out T? value);
    Task<ICacheResult<T>> TryGetAsync<T>(string key);
    Task<TimeSpan?> GetTTLAsync(string key);
    Task<bool> SetExpirationAsync(string key, TimeSpan? expiry);
    Task<bool> ExistsAsync(string key);
    Task RemoveAsync(string key);
    bool IsConnected();
    Task ClearCacheAsync();
}
