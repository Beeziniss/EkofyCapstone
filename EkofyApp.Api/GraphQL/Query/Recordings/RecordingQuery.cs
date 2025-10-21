using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.Uploads;
using EkofyApp.Application.ServiceInterfaces.Recordings;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Recordings;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class RecordingQuery(IRecordingService recordingService, IRedisCacheService redisCacheService)
{
    private readonly IRecordingService _recordingService = recordingService;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;

    [AuthorizeRoles(HelperRoleBase.ArtistModeratorAdminRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Recording>]
    public IQueryable<Recording> GetRecordings()
    {
        return _recordingService.GetRecordings();
    }

    [AuthorizeRoles(HelperRoleBase.ArtistModeratorAdminRoles)]
    [UseProjection]
    public async Task<RecordingTempRequest> GetMetadataRecordingUploadRequestAsync(string uploadId)
    {
        ICacheResult<CombinedUploadRequest> cacheResult = await _redisCacheService.TryGetGenericAsync<CombinedUploadRequest>($"upload:{uploadId}:requestUpload");
        if (!cacheResult.Success)
        {
            throw new NotFoundCustomException("Upload request not found or expired.");
        }

        return cacheResult.Value!.Recording;
    }
}
