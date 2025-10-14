using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;

namespace EkofyApp.Infrastructure.ThirdPartyServices.Redis;

/// <summary>
/// Represents a paginated cache result with total count information
/// </summary>
/// <typeparam name="T">The type of items in the paginated result</typeparam>
public sealed class PaginatedCacheResult<T> : ICacheResult<PaginatedData<T>>
{
    public bool Success { get; init; }
    public PaginatedData<T>? Value { get; init; }
    public TimeSpan? TimeToLive { get; init; }

    private PaginatedCacheResult(bool success, PaginatedData<T>? value, TimeSpan? ttl)
    {
        Success = success;
        Value = value;
        TimeToLive = ttl;
    }

    public static ICacheResult<PaginatedData<T>> Fail()
    {
        return new PaginatedCacheResult<T>(false, default, null);
    }

    public static ICacheResult<PaginatedData<T>> From(IEnumerable<T> items, int totalCount, TimeSpan? ttl = null)
    {
        var data = new PaginatedData<T>
        {
            Items = items,
            TotalCount = totalCount
        };
        return new PaginatedCacheResult<T>(true, data, ttl);
    }
}
