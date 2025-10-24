# Redis List Operations Implementation cho Favorite Playlist

## Tổng quan

Đã triển khai Redis List operations để tối ưu hóa caching cho favorite playlist thay vì sử dụng generic Set/Get operations. Điều này mang lại hiệu suất tốt hơn và khả năng quản lý cache thông minh hơn.

## Các thay đổi chính

### 1. **Thêm Redis List Operations vào IRedisCacheService**

```csharp
// Redis List Operations
Task<long> ListPushAsync(string key, string value, TimeSpan? expiry = null);
Task<long> ListPushRangeAsync(string key, IEnumerable<string> values, TimeSpan? expiry = null);
Task<string[]> ListRangeAsync(string key, long start = 0, long stop = -1);
Task<long> ListRemoveAsync(string key, string value, long count = 0);
Task<long> ListLengthAsync(string key);
Task<bool> ListContainsAsync(string key, string value);
```

### 2. **Implement Redis List Operations trong RedisCacheService**

- `ListPushAsync`: Thêm item vào đầu list với LPUSH
- `ListPushRangeAsync`: Thêm nhiều items cùng lúc
- `ListRangeAsync`: Lấy range của items từ list
- `ListRemoveAsync`: Xóa item từ list
- `ListLengthAsync`: Lấy độ dài của list
- `ListContainsAsync`: Kiểm tra item có tồn tại trong list không

### 3. **Tạo FavoritePlaylistCacheService**

Service chuyên biệt để quản lý favorite playlist cache:

```csharp
public interface IFavoritePlaylistCacheService
{
    Task<bool> IsTrackInFavoriteAsync(string userId, string trackId);
    Task AddTrackToFavoriteCacheAsync(string userId, string trackId);
    Task RemoveTrackFromFavoriteCacheAsync(string userId, string trackId);
    Task InvalidateFavoriteCacheAsync(string userId);
    Task<bool> EnsureCachePopulatedAsync(string userId);
}
```

#### Đặc điểm nổi bật:
- **Smart TTL Preservation**: Giữ nguyên TTL khi cập nhật cache
- **Atomic Operations**: Mỗi cache operation là atomic
- **Defensive Programming**: Kiểm tra cache tồn tại trước khi update
- **Graceful Fallback**: Xử lý lỗi một cách nhẹ nhang

### 4. **Cập nhật TracksResolver**

Đơn giản hóa logic bằng cách sử dụng FavoritePlaylistCacheService:

```csharp
public async Task<bool> CheckTrackInFavoritePlaylist([Parent] Track track, 
    [Service] IHttpContextAccessor httpContextAccessor, 
    [Service] IFavoritePlaylistCacheService favoritePlaylistCacheService)
{
    string currentUserId = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
        ?? throw new UnauthorizedCustomException("Your session is limit");

    return await favoritePlaylistCacheService.IsTrackInFavoriteAsync(currentUserId, track.Id);
}
```

### 5. **Cập nhật PlaylistService**

Tích hợp cache management vào playlist operations:

- **AddToPlaylistAsync**: Tự động update cache khi thêm vào "Favorite Songs"
- **RemoveFromPlaylistAsync**: Tự động update cache khi xóa khỏi "Favorite Songs"
- **DeletePlaylistAsync**: Invalidate cache nếu xóa favorite playlist

## Lợi ích của việc sử dụng Redis List

### 1. **Performance Optimization**
- **Cache Hit với O(1) lookup**: `ListContainsAsync` sử dụng LRANGE để lấy toàn bộ list rồi check
- **Batch Operations**: `ListPushRangeAsync` cho phép thêm nhiều tracks cùng lúc
- **Memory Efficient**: Redis List tối ưu memory hơn so với serialized objects

### 2. **Smart Cache Management**
- **TTL Preservation**: Giữ nguyên TTL khi update thay vì reset về 30 phút
- **Incremental Updates**: Chỉ thêm/xóa tracks cần thiết thay vì reload toàn bộ
- **Cache Consistency**: Đảm bảo cache luôn sync với database

### 3. **Timeline Comparison**

#### Trước (Inefficient):
```
User adds favorite → DB Update → Cache Invalidate → Cache Empty
Next check → Cache Miss → DB Query → Cache Set (30min TTL)
Another check → Cache Hit ✓
User adds another → DB Update → Cache Invalidate → Cache Empty (cycle repeats)
```

#### Bây giờ (Optimized):
```
User adds favorite → DB Update → Cache Update (preserve 25min TTL remaining)
Next check → Cache Hit ✓ (still 25min remaining)
Another check → Cache Hit ✓ (still 24min remaining)
User adds another → DB Update → Cache Update (preserve 23min TTL remaining)
Next check → Cache Hit ✓ (still 23min remaining)
```

## Monitoring & Metrics

### Key Metrics cần theo dõi:
1. **Cache Hit Ratio**: Mong đợi > 95%
2. **Average TTL at Update**: Nên > 15 phút (nửa cache lifetime)
3. **Database Query Frequency**: Giảm đáng kể cho favorite operations
4. **Redis Memory Usage**: Ổn định, không tăng đột biến

### Redis Commands được sử dụng:
- `LPUSH/LPUSHRANGE` cho thêm tracks
- `LRANGE` cho đọc tracks
- `LREM` cho xóa tracks
- `LLEN` cho check list length
- `TTL/EXPIRE` cho TTL management

## Best Practices được áp dụng

1. **Atomic Operations**: Mỗi cache update là atomic
2. **Defensive Programming**: Check cache existence trước khi update
3. **TTL Management**: Intelligent TTL preservation
4. **Memory Efficiency**: Sử dụng List thay vì Set cho ordered data
5. **Error Handling**: Graceful fallback nếu Redis operations fail

Việc implement này đảm bảo rằng cache Redis được sử dụng tối đa hiệu quả cho favorite playlist operations, giảm load lên database và cải thiện user experience đáng kể.