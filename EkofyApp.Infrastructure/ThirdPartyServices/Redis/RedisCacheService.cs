using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.Models.Listeners;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EkofyApp.Infrastructure.ThirdPartyServices.Redis;
public sealed class RedisCacheService(IDatabase redisDb, ILogger<RedisCacheService> logger) : IRedisCacheService
{
    private readonly IDatabase _redisDb = redisDb;
    private readonly ILogger<RedisCacheService> _logger = logger;

    // Configure JsonSerializerOptions to serialize enums as strings
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false
    };

    #region Default Methods
    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            await _redisDb.StringSetAsync(key, value, expiry, when: When.Always);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Redis] Set failed. Key: {key}");
        }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            RedisValue value = await _redisDb.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Redis] Get failed. Key: {key}");
            return null;
        }
    }

    public bool TryGetString(string key, out string? value)
    {
        try
        {
            RedisValue redisValue = _redisDb.StringGet(key);
            if (redisValue.HasValue)
            {
                value = redisValue.ToString();
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[Redis] TryGetString failed. Key: {key}");
        }

        value = default;

        return false;
    }

    public async Task<ICacheResult<string>> TryGetStringAsync(string key)
    {
        try
        {
            RedisValue value = await _redisDb.StringGetAsync(key);

            if (value.HasValue)
            {
                TimeSpan? ttl = await GetTTLAsync(key);
                return CacheResult<string>.From(value.ToString(), ttl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[Redis] TryGetString failed. Key: {key}");
        }

        return CacheResult<string>.Fail();
    }

    public string[] GetAllKeysByPattern(string pattern)
    {
        try
        {
            IServer server = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints().First());

            RedisKey[] keys = server.Keys(_redisDb.Database, pattern: pattern).ToArray();

            //chuyển RedisKey[] sang string[]
            return keys.Select(key => key.ToString()).ToArray();
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, $"[Redis] GetAllKeysByPattern failed. Pattern: {pattern}");
            return Array.Empty<string>();
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await _redisDb.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Redis] Exists failed. Key: {key}");
            return false;
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _redisDb.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Redis] Remove failed. Key: {key}");
        }
    }

    public TimeSpan? GetTTL(string key)
    {
        try
        {
            return _redisDb.KeyTimeToLive(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Redis] GetExpiration failed. Key: {key}");
            return null;
        }
    }

    public async Task<TimeSpan?> GetTTLAsync(string key)
    {
        try
        {
            return await _redisDb.KeyTimeToLiveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Redis] GetExpiration failed. Key: {key}");
            return null;
        }
    }

    public async Task<bool> SetExpirationAsync(string key, TimeSpan? expiry)
    {
        try
        {
            if (expiry.HasValue)
            {
                return await _redisDb.KeyExpireAsync(key, expiry.Value);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Redis] SetExpiration failed. Key: {key}");
            return false;
        }
    }

    public async Task ClearCacheAsync()
    {
        try
        {
            var server = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints()[0]);
            await server.FlushDatabaseAsync(_redisDb.Database);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] ClearCache failed.");
        }
    }

    public bool IsConnected()
    {
        try
        {
            return _redisDb.Multiplexer.IsConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] IsConnected failed.");
            return false;
        }
    }
    #endregion

    #region Generic Methods
    public async Task SetGenericAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            string json = JsonSerializer.Serialize(value, _jsonOptions);
            await _redisDb.StringSetAsync(key, json, expiry, when: When.Always);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Redis] SetStringAsync failed. Key: {key}");
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            RedisValue json = await _redisDb.StringGetAsync(key);
            if (json.HasValue)
            {
                return JsonSerializer.Deserialize<T>(json!, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Redis] GetStringAsync failed. Key: {key}");
        }

        return default;
    }

    public bool TryGetGeneric<T>(string key, out T? value)
    {
        try
        {
            RedisValue json = _redisDb.StringGet(key);
            if (json.HasValue)
            {
                value = JsonSerializer.Deserialize<T>(json!, _jsonOptions);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[Redis] TryGetString failed. Key: {key}");
        }

        value = default;

        return false;
    }

    public async Task<ICacheResult<T>> TryGetGenericAsync<T>(string key)
    {
        try
        {
            RedisValue json = await _redisDb.StringGetAsync(key);
            if (json.HasValue)
            {
                T? value = JsonSerializer.Deserialize<T>(json!, _jsonOptions);
                TimeSpan? ttl = await GetTTLAsync(key);
                return CacheResult<T>.From(value!, ttl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[Redis] TryGetStringAsync failed. Key: {key}");
        }

        return CacheResult<T>.Fail();
    }
    #endregion

    #region Hash Methods
    [Obsolete("Chưa kiểm tra và hàm này chưa đúng mục đích.")]
    public async Task<ICacheResult<Dictionary<string, string?>>> TryGetHashManyAsync(string key, params string[] fields)
    {
        Dictionary<string, string?> result = [];

        try
        {
            RedisValue[] redisFields = fields.Select(f => (RedisValue)f).ToArray();
            RedisValue[] values = await _redisDb.HashGetAsync(key, redisFields);

            result = fields.Zip(values, (f, v) => new { f, v })
                           .ToDictionary(x => x.f, x => (string?)x.v);

            TimeSpan? ttl = await GetTTLAsync(key);

            return CacheResult<Dictionary<string, string?>>.From(result, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting hash values from Redis for key {Key}", key);
        }

        return CacheResult<Dictionary<string, string?>>.Fail();
    }

    public async Task<string?> HashGetAsync(string key, string field)
    {
        try
        {
            RedisValue value = await _redisDb.HashGetAsync(key, field);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting hash value from Redis for key {Key}, field {Field}", key, field);
            return null;
        }
    }

    public async Task HashSetAsync(string key, Dictionary<string, string?> fields, TimeSpan? expiry = null)
    {
        try
        {
            // Convert Dictionary to HashEntry array
            HashEntry[] hashEntries = fields.Select(kvp => new HashEntry(kvp.Key, kvp.Value is null ? RedisValue.Null : kvp.Value)).ToArray();

            // Set multiple hash fields at once
            await _redisDb.HashSetAsync(key, hashEntries);

            // Set expiration if provided
            if (expiry.HasValue)
            {
                await _redisDb.KeyExpireAsync(key, expiry.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when setting hash fields to Redis for key {Key}", key);
        }
    }

    public async Task HashDeleteAsync(string key)
    {
        try
        {
            await _redisDb.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when deleting hash in Redis for key {Key}", key);
        }
    }

    public async Task<bool> HashFieldExistsAsync(string key, string field)
    {
        try
        {
            return await _redisDb.HashExistsAsync(key, field);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when checking hash field existence in Redis for key {Key}, field {Field}", key, field);
            return false;
        }
    }

    public async Task<bool> HashFieldDeleteAsync(string key, string field)
    {
        try
        {
            return await _redisDb.HashDeleteAsync(key, field);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when deleting hash field in Redis for key {Key}, field {Field}", key, field);
            return false;
        }
    }

    public async Task<HashEntry[]?> HashGetAllAsync(string key) {        
        try
        {
            HashEntry[] entries = await _redisDb.HashGetAllAsync(key);
            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting all hash values from Redis for key {Key}", key);
            return null;
        }
    }

    public async Task<long> HashIncrementAsync(string key, string field, long incrementBy = 1)
    {
        try
        {
            // Redis sẽ tạo field nếu chưa tồn tại
            long newValue = await _redisDb.HashIncrementAsync(key, field, incrementBy);
            RedisValue[] fieldValues = [field];

            //await _redisDb.HashFieldExpireAsync(key, fieldValues, TimeSpan.FromMinutes(6));
            //await _redisDb.KeyExpireAsync(key, TimeSpan.FromMinutes(30));

            return newValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when incrementing hash value in Redis for key {Key}, field {Field}", key, field);
            return -1; // hoặc throw
        }
    }

    public async Task HashDecrementAsync(string key, string field, long decrementBy = 1)
    {
        try
        {
            // Redis sẽ tạo field nếu chưa tồn tại
            long newValue = await _redisDb.HashDecrementAsync(key, field, decrementBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when incrementing hash value in Redis for key {Key}, field {Field}", key, field);
        }
    }

    public async Task<bool> HashFieldExpireAsync(string key, string field, TimeSpan? expiry)
    {
        try
        {
            if (expiry.HasValue)
            {
                RedisValue[] fieldValues = [field];
                await _redisDb.HashFieldExpireAsync(key, fieldValues, expiry.Value);

                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when setting hash field expiration in Redis for key {Key}, field {Field}", key, field);
            return false;
        }
    }

    [Obsolete("Chưa kiểm tra và hàm này chưa đúng mục đích.")]
    public async Task<bool> SetHashManyAsync(string key, Dictionary<string, string?> fields, TimeSpan? expiry = null)
    {
        try
        {
            // Convert Dictionary<string, string?> thành HashEntry[]
            HashEntry[] hashEntries = fields.Select(kvp => new HashEntry(kvp.Key, kvp.Value ?? string.Empty)).ToArray();

            // Ghi nhiều field vào hash
            await _redisDb.HashSetAsync(key, hashEntries);

            // Nếu có TTL thì set luôn
            if (expiry.HasValue)
            {
                await _redisDb.KeyExpireAsync(key, expiry);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when setting hash values to Redis for key {Key}", key);
        }

        return false;
    }
    #endregion

    public async Task<ICacheResult<IEnumerable<TrackTempRequest>>> GetPendingTrackUploadsAsync(int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            IServer server = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints().First());
            RedisKey[] keys = server.Keys(_redisDb.Database, pattern: "track:*:requestUpload").ToArray();

            List<TrackTempRequest> allRequests = [];

            foreach (RedisKey key in keys)
            {
                try
                {
                    RedisValue value = await _redisDb.StringGetAsync(key);

                    if (value.HasValue)
                    {
                        TrackTempRequest? request = JsonSerializer.Deserialize<TrackTempRequest>(value!, _jsonOptions);
                        if (request != null)
                        {
                            allRequests.Add(request);
                        }
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogWarning(innerEx, $"[Redis] Failed to deserialize track upload request. Key: {key}");
                }
            }

            IEnumerable<TrackTempRequest> paged = allRequests.OrderBy(r => r.RequestedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return CacheResult<IEnumerable<TrackTempRequest>>.From(paged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] Failed to get pending track uploads.");
            return CacheResult<IEnumerable<TrackTempRequest>>.Fail();
        }
    }

    public async Task<ICacheResult<IEnumerable<PendingArtistRegistrationRequest>>> GetPendingArtistRegistrationsAsync(int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            IServer server = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints().First());
            RedisKey[] keys = server.Keys(_redisDb.Database, pattern: "artist:*:pendingRegistration").ToArray();

            List<PendingArtistRegistrationRequest> allRequests = [];

            foreach (RedisKey key in keys)
            {
                try
                {
                    RedisValue value = await _redisDb.StringGetAsync(key);

                    if (value.HasValue)
                    {
                        PendingArtistRegistrationRequest? request = JsonSerializer.Deserialize<PendingArtistRegistrationRequest>(value!, _jsonOptions);
                        if (request != null)
                        {
                            allRequests.Add(request);
                        }
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogWarning(innerEx, $"[Redis] Failed to deserialize artist registration request. Key: {key}");
                }
            }

            IEnumerable<PendingArtistRegistrationRequest> paged = allRequests.OrderBy(r => r.RequestedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return CacheResult<IEnumerable<PendingArtistRegistrationRequest>>.From(paged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] Failed to get pending artist registrations.");
            return CacheResult<IEnumerable<PendingArtistRegistrationRequest>>.Fail();
        }
    }

    public async Task<ICacheResult<IEnumerable<PendingListenerRegistration>>> GetPendingListenerRegistrationsAsync(int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            IServer server = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints().First());
            RedisKey[] keys = server.Keys(_redisDb.Database, pattern: "listener:*:pendingRegistration").ToArray();

            List<PendingListenerRegistration> allRequests = [];

            foreach (RedisKey key in keys)
            {
                try
                {
                    RedisValue value = await _redisDb.StringGetAsync(key);

                    if (value.HasValue)
                    {
                        PendingListenerRegistration? request = JsonSerializer.Deserialize<PendingListenerRegistration>(value!, _jsonOptions);
                        if (request != null)
                        {
                            allRequests.Add(request);
                        }
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogWarning(innerEx, $"[Redis] Failed to deserialize listener registration request. Key: {key}");
                }
            }

            IEnumerable<PendingListenerRegistration> paged = allRequests.OrderBy(r => r.RequestedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return CacheResult<IEnumerable<PendingListenerRegistration>>.From(paged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] Failed to get pending listener registrations.");
            return CacheResult<IEnumerable<PendingListenerRegistration>>.Fail();
        }
    }
}
