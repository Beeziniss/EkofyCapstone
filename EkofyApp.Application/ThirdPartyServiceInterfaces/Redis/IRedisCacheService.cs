using EkofyApp.Application.Models.Tracks;
using StackExchange.Redis;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
public interface IRedisCacheService
{
    Task SetAsync(string key, string value, bool overrides, TimeSpan? expiry = null);
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
