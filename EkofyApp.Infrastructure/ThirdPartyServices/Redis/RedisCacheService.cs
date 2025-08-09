using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace EkofyApp.Infrastructure.ThirdPartyServices.Redis;
public sealed class RedisCacheService(IDatabase redisDb, ILogger<RedisCacheService> logger) : IRedisCacheService
{
    private readonly IDatabase _redisDb = redisDb;
    private readonly ILogger<RedisCacheService> _logger = logger;

    public IDatabase RedisDb => _redisDb;

    #region Default Methods
    public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            await _redisDb.StringSetAsync(key, value, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Redis] Set failed. Key: {key}");
        }
    }

    public async Task<string?> GetAsync(string key)
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

    public bool TryGet(string key, out string? value)
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
            _logger.LogWarning(ex, $"[Redis] TryGet failed. Key: {key}");
        }

        value = default;

        return false;
    }

    public async Task<ICacheResult<string>> TryGetAsync(string key)
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
            _logger.LogWarning(ex, $"[Redis] TryGet failed. Key: {key}");
        }

        return CacheResult<string>.Fail();
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
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            string json = JsonSerializer.Serialize(value);
            await _redisDb.StringSetAsync(key, json, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Redis] SetAsync failed. Key: {key}");
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            RedisValue json = await _redisDb.StringGetAsync(key);
            if (json.HasValue)
            {
                return JsonSerializer.Deserialize<T>(json!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Redis] GetAsync failed. Key: {key}");
        }

        return default;
    }

    public bool TryGet<T>(string key, out T? value)
    {
        try
        {
            RedisValue json = _redisDb.StringGet(key);
            if (json.HasValue)
            {
                value = JsonSerializer.Deserialize<T>(json!);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[Redis] TryGet failed. Key: {key}");
        }

        value = default;

        return false;
    }

    public async Task<ICacheResult<T>> TryGetAsync<T>(string key)
    {
        try
        {
            RedisValue json = await _redisDb.StringGetAsync(key);
            if (json.HasValue)
            {
                T? value = JsonSerializer.Deserialize<T>(json!);
                TimeSpan? ttl = await GetTTLAsync(key);
                return CacheResult<T>.From(value!, ttl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[Redis] TryGetAsync failed. Key: {key}");
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
}
