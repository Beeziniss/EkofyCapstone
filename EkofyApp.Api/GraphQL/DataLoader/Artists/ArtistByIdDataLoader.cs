using EkofyApp.Domain.Entities;
using HealthyNutritionApp.Application.Interfaces;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.DataLoader.Artists;

public class ArtistByIdDataLoader(IBatchScheduler scheduler, DataLoaderOptions options, IUnitOfWork unitOfWork) : BatchDataLoader<string, Artist>(scheduler, options)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    protected override async Task<IReadOnlyDictionary<string, Artist>> LoadBatchAsync(
        IReadOnlyList<string> keys, CancellationToken ct)
    {
        List<Artist> result = await _unitOfWork.GetCollection<Artist>().Find(x => keys.Contains(x.Id)).ToListAsync(ct);
        return result.ToDictionary(a => a.Id);
    }
}

