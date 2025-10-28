using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Uploads;
using EkofyApp.Application.ServiceInterfaces.TrackComments;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using HotChocolate.Authorization;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Tracks;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public class TrackQuery(ITrackService trackService, ITrackCommentService trackCommentService, IRedisCacheService redisCacheService, IAmazonCloudFrontService amazonCloudFrontService)
{
    private readonly ITrackService _trackService = trackService;
    private readonly ITrackCommentService _trackCommentService = trackCommentService;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;
    private readonly IAmazonCloudFrontService _amazonCloudFrontService = amazonCloudFrontService;

    //[AuthorizeRoles(HelperRoleBase.FullRoles)]
    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Track>]
    public IQueryable<Track> GetTracks()
    {
        return _trackService.GetTracks();
    }

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Track>]
    public IQueryable<Track> SearchTracks(string name)
    {
        return _trackService.SearchTracks(name);
    }

    [AuthorizeRoles(HelperRoleBase.ModeratorAdminRoles)]
    [UseProjection]
    public async Task<PaginatedData<CombinedUploadRequest>> GetPendingTrackUploadRequestsAsync(int pageNumber = 1, int pageSize = 20)
    {
        return await _trackService.GetPendingTrackUploadRequestsAsync(pageNumber, pageSize);
    }

    [AuthorizeRoles(HelperRoleBase.ModeratorAdminRoles)]
    [UseProjection]
    public async Task<CombinedUploadRequest> GetPendingTrackUploadRequestByIdAsync(string uploadId)
    {
        return await _trackService.GetPendingTrackUploadRequestByIdAsync(uploadId);
    }

    [AuthorizeRoles(HelperRoleBase.ModeratorAdminRoles)]
    [UseProjection]
    public async Task<TrackTempRequest> GetMetadataTrackUploadRequestAsync(string uploadId)
    {
        ICacheResult<CombinedUploadRequest> cacheResult = await _redisCacheService.TryGetGenericAsync<CombinedUploadRequest>($"upload:{uploadId}:requestUpload");
        if (!cacheResult.Success)
        {
            throw new NotFoundCustomException("Upload request not found or expired.");
        }

        return cacheResult.Value!.Track;
    }

    [AuthorizeRoles(HelperRoleBase.ModeratorAdminRoles)]
    public string GetOriginalFileTrackUploadRequest(string trackId)
    {
        return _amazonCloudFrontService.GenerateOriginalSignedURL(trackId);
    }

    [AuthorizeRoles(HelperRoleBase.ListenerArtistRoles)]
    [UseProjection]
    public async Task<IEnumerable<Track>> GetTrackBySemanticSearch(string term)
    {
        return await _trackService.GetAllTracksBySemanticAsync(term);
    }

    #region Original
    //public async Task<TrackResponse> GetTrackByIdAsync(string id, IResolverContext context, [Service] IUnitOfWork unitOfWork, [Service] IMapper mapper)
    //{
    //    IReadOnlyList<string> selectedFields = GetSelectedFieldNames(context);
    //    ProjectionDefinition<Track> projection = BuildProjection<Track>(selectedFields);

    //    Track tracks = await unitOfWork.GetCollection<Track>()
    //        .Find(x => x.UserId == id)
    //        .Project<Track>(projection)
    //        .FirstOrDefaultAsync();

    //    return mapper.Map<TrackResponse>(tracks);
    //}

    //public async Task<TrackResponse> GetTrackByIdAsync(string id, IResolverContext context)
    //{
    //    IReadOnlyList<string> selectedFields = GetSelectedFieldNames(context);
    //    ProjectionDefinition<Track> projection = BuildProjection<Track>(selectedFields);

    //    return await _trackService.GetTrackResolverContext(projection, id);
    //}

    //public IReadOnlyList<string> GetSelectedFieldNames(IResolverContext context)
    //{
    //    return context.Selection.SyntaxNode.SelectionSet?.Selections
    //        .OfType<FieldNode>()
    //        .Select(f => f.DisplayName.Value)
    //        .Distinct()
    //        .ToList()
    //        ?? [];
    //}

    //public ProjectionDefinition<T> BuildProjection<T>(IEnumerable<string> fields)
    //{
    //    ProjectionDefinitionBuilder<T> builder = Builders<T>.Projection;
    //    ProjectionDefinition<T> projection = builder.Include("_id"); // luôn cần _id

    //    foreach (string field in fields)
    //    {
    //        // Lấy thông tin property từ class T (ignore case: "name" → "DisplayName")
    //        PropertyInfo? propInfo = typeof(T).GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
    //        if (propInfo != null)
    //        {
    //            BsonElementAttribute? bsonElement = propInfo.GetCustomAttribute<BsonElementAttribute>();
    //            string fieldName = bsonElement?.ElementName ?? propInfo.DisplayName;

    //            projection = projection.Include(fieldName);
    //        }
    //    }

    //    return projection;
    //}
    #endregion
}
