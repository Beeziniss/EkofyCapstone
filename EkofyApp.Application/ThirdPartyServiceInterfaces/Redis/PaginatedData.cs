namespace EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;

/// <summary>
/// Chứa dữ liệu phân trang với tổng số
/// </summary>
/// <typeparam name="T">The type of items</typeparam>
public sealed class PaginatedData<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int TotalCount { get; init; }
}
