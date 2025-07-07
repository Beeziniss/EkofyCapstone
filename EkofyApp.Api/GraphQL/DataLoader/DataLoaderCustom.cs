using EkofyApp.Domain.Entities;
using HealthyNutritionApp.Application.Interfaces;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.DataLoader;

public class DataLoaderCustom<T>(IBatchScheduler scheduler, DataLoaderOptions options, IUnitOfWork unitOfWork) : BatchDataLoader<string, T>(scheduler, options)
    where T : class, IEntityCustom
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    protected override async Task<IReadOnlyDictionary<string, T>> LoadBatchAsync(
        IReadOnlyList<string> keys, CancellationToken ct)
    {
        List<T> result = await _unitOfWork.GetCollection<T>().Find(x => keys.Contains(x.Id)).ToListAsync(ct);
        return result.ToDictionary(a => a.Id);
    }
}
