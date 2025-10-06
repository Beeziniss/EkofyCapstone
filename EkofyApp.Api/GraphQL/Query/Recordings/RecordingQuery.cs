using EkofyApp.Application.Models.Recordings;
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

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Recording>]
    public IQueryable<Recording> GetRecordingsQueryable()
    {
        return _recordingService.GetRecordingsQueryable();
    }

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseProjection]
    public async Task<RecordingTempRequest> GetMetadataRecordingUploadRequestAsync(string recordingId)
    {
        ICacheResult<RecordingTempRequest> cacheResult = await _redisCacheService.TryGetGenericAsync<RecordingTempRequest>($"recording:{recordingId}:requestUpload");
        if (!cacheResult.Success)
        {
            throw new NotFoundCustomException("RecordingProjection upload request not found or expired.");
        }

        return cacheResult.Value!;
    }
}
