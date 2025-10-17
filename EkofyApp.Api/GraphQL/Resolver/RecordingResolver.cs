using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;
[ExtendObjectType(typeof(Recording))]
public sealed class RecordingResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Track> GetTrack([Parent] Recording recording, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Track>().AsQueryable().Where(t => t.Id == recording.TrackId);
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUsers([Parent] Recording recording, [Service] IUnitOfWork unitOfWork)
    {
        IEnumerable<string> userIds = recording.RecordingSplits.Select(rs => rs.UserId).ToList();
        return unitOfWork.GetCollection<User>().AsQueryable().Where(u => userIds.Contains(u.Id));
    }
}
