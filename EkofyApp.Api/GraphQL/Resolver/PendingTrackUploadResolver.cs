using EkofyApp.Application.Models.Uploads;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(CombinedUploadRequest))]
public sealed class PendingTrackUploadResolver
{
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetWorkUsers([Parent] CombinedUploadRequest combinedUploadRequest, [Service] IUnitOfWork unitOfWork)
    {
        IEnumerable<string> userIds = combinedUploadRequest.Work.WorkSplits.Select(ws => ws.UserId).ToList();
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => userIds.Contains(x.Id));
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetRecordingUsers([Parent] CombinedUploadRequest combinedUploadRequest, [Service] IUnitOfWork unitOfWork)
    {
        IEnumerable<string> userIds = combinedUploadRequest.Recording.RecordingSplitRequests.Select(rs => rs.UserId).ToList();
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => userIds.Contains(x.Id));
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Artist> GetMainArtists([Parent] CombinedUploadRequest combinedUploadRequest, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Artist>().AsQueryable().Where(x => combinedUploadRequest.Track.MainArtistIds.Contains(x.Id));
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Artist> GetFeaturedArtists([Parent] CombinedUploadRequest combinedUploadRequest, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Artist>().AsQueryable().Where(x => combinedUploadRequest.Track.FeaturedArtistIds.Contains(x.Id));
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Category> GetCategories([Parent] Track track, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Category>().AsQueryable().Where(x => track.CategoryIds.Contains(x.Id));
    }
}
