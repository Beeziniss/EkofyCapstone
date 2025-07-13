using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Infrastructure.Services;
using HealthyNutritionApp.Application.Interfaces;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.DataLoader.Artists;

public sealed class ArtistDataLoader(IBatchScheduler scheduler, DataLoaderOptions options, IUnitOfWork unitOfWork, IRedisCacheService redisCacheService) : DataLoaderCustomOneToOne<Artist>(scheduler, options, unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;

    // Đây là DataLoader cho Artist, kế thừa từ DataLoaderCustomOneToOne
    // Không nhất thiết phải override LoadBatchAsync nếu không cần logic đặc biệt
    protected override async Task<IReadOnlyDictionary<string, Artist>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        // Ví dụ logic riêng: chỉ lấy Artist có "IsBanned" là false
        FilterDefinition<Artist> filter = Builders<Artist>.Filter.And(
            Builders<Artist>.Filter.In(a => a.Id, keys)
        //Builders<Artist>.Filter.Eq(a => a.IsBanned, false) // chỉ lấy active
        );

        // Caching
        // Kiêm tra xem đã có cache chưa
        //if (_redisCacheService.TryGet("artists:ids:many", out IEnumerable<Artist>? values))
        //{
        //    // Nếu có cache thì trả về ngay
        //    Console.WriteLine("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        //    return values!.ToDictionary(a => a.Id);
        //}

        IEnumerable<Artist> activeArtists = await _unitOfWork.GetCollection<Artist>().Find(filter).ToListAsync(cancellationToken);

        //await _redisCacheService.SetAsync("artists:ids:many", activeArtists, TimeSpan.FromMinutes(5));

        return activeArtists.ToDictionary(a => a.Id);
    }
}

