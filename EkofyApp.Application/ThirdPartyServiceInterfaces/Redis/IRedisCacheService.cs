using EkofyApp.Application.Models.Tracks;
using StackExchange.Redis;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
public interface IRedisCacheService
{
    Task SetStringAsync(string key, string value, TimeSpan? expiry = null);
    Task SetGenericAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<string?> GetStringAsync(string key);
    bool TryGetString(string key, out string? value);
    Task<ICacheResult<string>> TryGetStringAsync(string key);
    bool TryGetGeneric<T>(string key, out T? value);
    Task<ICacheResult<T>> TryGetGenericAsync<T>(string key);
    Task<TimeSpan?> GetTTLAsync(string key);
    Task<bool> SetExpirationAsync(string key, TimeSpan? expiry);
    Task<bool> ExistsAsync(string key);
    Task RemoveAsync(string key);
    bool IsConnected();
    Task ClearCacheAsync();

    [Obsolete("Chưa kiểm tra và hàm này chưa đúng mục đích.")]
    Task<ICacheResult<Dictionary<string, string?>>> TryGetHashManyAsync(string key, params string[] fields);

    [Obsolete("Chưa kiểm tra và hàm này chưa đúng mục đích.")]
    Task<bool> SetHashManyAsync(string key, Dictionary<string, string?> fields, TimeSpan? expiry = null);
    Task<ICacheResult<IEnumerable<TrackTempRequest>>> GetPendingTrackUploadsAsync(int pageNumber = 1, int pageSize = 20);
    Task<string?> HashGetAsync(string key, string field);
    Task<long> HashIncrementAsync(string key, string field, long incrementBy = 1);
    Task<bool> HashFieldExpireAsync(string key, string field, TimeSpan? expiry);
    Task HashSetAsync(string key, Dictionary<string, string?> fields, TimeSpan? expiry = null);
    Task HashDeleteAsync(string key);
    Task<bool> HashFieldExistsAsync(string key, string field);
    Task<bool> HashFieldDeleteAsync(string key, string field);
    string[] GetAllKeysByPattern(string pattern);
    Task<HashEntry[]?> HashGetAllAsync(string key);
    Task HashDecrementAsync(string key, string field, long decrementBy = 1);
}
