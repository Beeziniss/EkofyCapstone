using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.MonthlyStreamCounts;
using EkofyApp.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.MonthlyStreamCounts;
public sealed class MonthlyStreamCountService(IUnitOfWork unitOfWork) : IMonthlyStreamCountService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<MonthlyStreamCount> GetMonthlyStreamCounts()
    {
        return _unitOfWork.GetCollection<MonthlyStreamCount>().AsQueryable();
    }

    public async Task UpsertMonthlyStreamCountAsync(string trackId, long streamCount, int month, int year, CancellationToken cancellationToken = default)
    {
        FilterDefinition<MonthlyStreamCount> filter = Builders<MonthlyStreamCount>.Filter.Where(m => m.TrackId == trackId && m.Month == month && m.Year == year && m.ProcessedAt == null);

        UpdateDefinition<MonthlyStreamCount> updateDefinition = Builders<MonthlyStreamCount>.Update
            .Inc(m => m.StreamCount, streamCount)
            .SetOnInsert(m => m.Id, ObjectId.GenerateNewId().ToString())
            .SetOnInsert(m => m.TrackId, trackId)
            .SetOnInsert(m => m.Month, month)
            .SetOnInsert(m => m.Year, year);

        await _unitOfWork.GetCollection<MonthlyStreamCount>().UpdateOneAsync(
            filter,
            updateDefinition,
            new UpdateOptions { IsUpsert = true },
            cancellationToken: cancellationToken
        );
    }
}
