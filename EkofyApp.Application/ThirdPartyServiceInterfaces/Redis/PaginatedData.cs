namespace EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;

/// <summary>
/// Contains paginated data with total count
/// </summary>
/// <typeparam name="T">The type of items</typeparam>
public sealed class PaginatedData<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int TotalCount { get; init; }
}
