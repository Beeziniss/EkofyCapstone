using EkofyApp.Domain.Entities;
using HealthyNutritionApp.Application.Interfaces;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.DataLoader;

public class DataLoaderCustomOneToMany<T>(IBatchScheduler scheduler, DataLoaderOptions options, IUnitOfWork unitOfWork) : BatchDataLoader<string, IEnumerable<T>>(scheduler, options)
    where T : class, IEntityCustom
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    protected override async Task<IReadOnlyDictionary<string, IEnumerable<T>>> LoadBatchAsync(
        IReadOnlyList<string> keys, CancellationToken ct)
    {
        IEnumerable<T> result = await _unitOfWork.GetCollection<T>()
            .Find(Builders<T>.Filter.In(a => a.Id, keys))
            .ToListAsync(ct);

        // Group the results by Id and convert to a dictionary
        return result
            .GroupBy(a => a.Id)
            .ToDictionary(g => g.Key, g => g.AsEnumerable()); ;
    }
}
