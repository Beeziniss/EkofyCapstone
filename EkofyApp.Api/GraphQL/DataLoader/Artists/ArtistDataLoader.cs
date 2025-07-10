using EkofyApp.Domain.Entities;
using EkofyApp.Infrastructure.Services;
using HealthyNutritionApp.Application.Interfaces;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.DataLoader.Artists;

public sealed class ArtistDataLoader(IBatchScheduler scheduler, DataLoaderOptions options, IUnitOfWork unitOfWork) : DataLoaderCustom<Artist>(scheduler, options, unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    // Đây là DataLoader cho Artist, kế thừa từ DataLoaderCustom
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

        IEnumerable<Artist> activeArtists = await _unitOfWork.GetCollection<Artist>().Find(filter).ToListAsync(cancellationToken);

        return activeArtists.ToDictionary(a => a.Id);
    }
}

