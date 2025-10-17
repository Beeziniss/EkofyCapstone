using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(MonthlyStreamCount))]
public sealed class MonthlyStreamCountResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Track> GetTrack([Parent] MonthlyStreamCount monthlyStreamCount, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Track>().AsQueryable().Where(t => t.Id == monthlyStreamCount.TrackId);
    }
}
