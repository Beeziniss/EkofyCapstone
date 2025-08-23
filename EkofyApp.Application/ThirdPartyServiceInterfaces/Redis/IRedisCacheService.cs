

using EkofyApp.Application.Models.Tracks;

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

    [Obsolete("Chưa kiểm tra và hàm này chưa đúng mục đích.")]
    Task<ICacheResult<Dictionary<string, string?>>> TryGetHashManyAsync(string key, params string[] fields);

    [Obsolete("Chưa kiểm tra và hàm này chưa đúng mục đích.")]
    Task<bool> SetHashManyAsync(string key, Dictionary<string, string?> fields, TimeSpan? expiry = null);
    Task<ICacheResult<IEnumerable<TrackTempRequest>>> GetPendingTrackUploadsAsync(int pageNumber = 1, int pageSize = 20);
}
