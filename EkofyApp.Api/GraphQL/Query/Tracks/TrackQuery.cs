using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Tracks;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public class TrackQuery(ITrackService trackService, IRedisCacheService redisCacheService, IAmazonCloudFrontService amazonCloudFrontService)
{
    private readonly ITrackService _trackService = trackService;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;
    private readonly IAmazonCloudFrontService _amazonCloudFrontService = amazonCloudFrontService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Track>]
    public IQueryable<Track> GetTracks()
    {
        return _trackService.GetTracksQueryable();
    }

    // TODO: Sorting for requests?
    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    public async Task<IEnumerable<TrackTempRequest>> GetPendingTrackUploadRequestsAsync()
    {
        ICacheResult<IEnumerable<TrackTempRequest>> requests = await _redisCacheService.GetPendingTrackUploadsAsync();
        if (requests.Success)
        {
            return requests.Value!;
        }

        return [];
    }

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseProjection]
    public async Task<TrackTempRequest> GetMetadataTrackUploadRequestAsync(string trackId)
    {
        ICacheResult<TrackTempRequest> cacheResult = await _redisCacheService.TryGetGenericAsync<TrackTempRequest>($"track:{trackId}:requestUpload");
        if (!cacheResult.Success)
        {
            throw new NotFoundCustomException("Track upload request not found or expired.");
        }

        return cacheResult.Value!;
    }

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseProjection]
    public string GetOriginalFileTrackUploadRequest(string trackId)
    {
        return _amazonCloudFrontService.GenerateOriginalSignedURL(trackId);
    }

    #region Original
    //public async Task<TrackResponse> GetTrackByIdAsync(string id, IResolverContext context, [Service] IUnitOfWork unitOfWork, [Service] IMapper mapper)
    //{
    //    IReadOnlyList<string> selectedFields = GetSelectedFieldNames(context);
    //    ProjectionDefinition<Track> projection = BuildProjection<Track>(selectedFields);

    //    Track tracks = await unitOfWork.GetCollection<Track>()
    //        .Find(x => x.Id == id)
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
