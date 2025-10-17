using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(RoyaltyReport))]
public sealed class RoyaltyReportResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Track> GetTrack([Parent] RoyaltyReport royaltyReport, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Track>().AsQueryable().Where(t => t.Id == royaltyReport.TrackId);
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUsers([Parent] RoyaltyReport royaltyReport, [Service] IUnitOfWork unitOfWork)
    {
        IEnumerable<string> userIds = royaltyReport.RoyaltySplits.Select(rs => rs.UserId).ToList();
        return unitOfWork.GetCollection<User>().AsQueryable().Where(u => userIds.Contains(u.Id));
    }
}
