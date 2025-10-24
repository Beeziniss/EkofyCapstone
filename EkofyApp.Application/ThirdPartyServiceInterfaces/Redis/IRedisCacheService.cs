using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Application.Models.Listeners;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Uploads;
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
    Task<ICacheResult<PaginatedData<TrackTempRequest>>> GetPendingTrackUploadsAsync(int pageNumber = 1, int pageSize = 20);
    Task<ICacheResult<PaginatedData<CombinedUploadRequest>>> GetPendingCombinedUploadsAsync(int pageNumber = 1, int pageSize = 20);
    Task<ICacheResult<PaginatedData<PendingArtistRegistrationRequest>>> GetPendingArtistRegistrationsAsync(int pageNumber = 1, int pageSize = 20);
    Task<ICacheResult<PaginatedData<PendingListenerRegistrationResponse>>> GetPendingListenerRegistrationsAsync(int pageNumber = 1, int pageSize = 20);
    Task<ICacheResult<PaginatedData<PendingArtistPackageResponse>>> GetPendingArtistPackagesAsync(int pageNumber = 1, int pageSize = 20);
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

    // Redis List Operations
    Task<long> ListPushAsync(string key, string value, TimeSpan? expiry = null);
    Task<long> ListPushRangeAsync(string key, IEnumerable<string> values, TimeSpan? expiry = null);
    Task<string[]> ListRangeAsync(string key, long start = 0, long stop = -1);
    Task<long> ListRemoveAsync(string key, string value, long count = 0);
    Task<long> ListLengthAsync(string key);
    Task<bool> ListContainsAsync(string key, string value);
}
