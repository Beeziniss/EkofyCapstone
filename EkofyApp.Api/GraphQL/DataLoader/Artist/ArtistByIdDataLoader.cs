using EkofyApp.Domain.Entities;
using HealthyNutritionApp.Application.Interfaces;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.DataLoader.Artist;

public class ArtistByIdDataLoader(IBatchScheduler scheduler, DataLoaderOptions options, IUnitOfWork unitOfWork) : BatchDataLoader<string, Artists>(scheduler, options)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    protected override async Task<IReadOnlyDictionary<string, Artists>> LoadBatchAsync(
        IReadOnlyList<string> keys, CancellationToken ct)
    {
        List<Artists> result = await _unitOfWork.GetCollection<Artists>().Find(x => keys.Contains(x.Id)).ToListAsync(ct);
        return result.ToDictionary(a => a.Id);
    }
}

