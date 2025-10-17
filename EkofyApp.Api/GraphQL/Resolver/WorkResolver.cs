using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Work))]
public sealed class WorkResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Track> GetTrack([Parent] Work work, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Track>().AsQueryable().Where(t => t.Id == work.TrackId);
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUsers([Parent] Work work, [Service] IUnitOfWork unitOfWork)
    {
        IEnumerable<string> userIds = work.WorkSplits.Select(ws => ws.UserId).ToList();
        return unitOfWork.GetCollection<User>().AsQueryable().Where(u => userIds.Contains(u.Id));
    }
}
