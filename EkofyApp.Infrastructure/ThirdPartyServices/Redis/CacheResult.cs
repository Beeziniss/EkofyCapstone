using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;

namespace EkofyApp.Infrastructure.ThirdPartyServices.Redis;
public sealed class CacheResult<T>(bool success, T? value, TimeSpan? ttl) : ICacheResult<T>
{
    public bool Success { get; init; } = success;
    public T? Value { get; init; } = value;
    public TimeSpan? TimeToLive { get; init; } = ttl;

    public static ICacheResult<T> Fail()
    {
        return new CacheResult<T>(false, default, null);
    }

    public static ICacheResult<T> From(T value, TimeSpan? ttl = null)
    {
        return new CacheResult<T>(true, value, ttl);
    }
}

