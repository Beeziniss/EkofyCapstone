using EkofyApp.Application.Models.Uploads;
using EkofyApp.Application.Models.Works;
using EkofyApp.Application.ServiceInterfaces.Works;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Works;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class WorkQuery(IWorkService workService, IRedisCacheService redisCacheService)
{
    private readonly IWorkService _workService = workService;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;

    [AuthorizeRoles(HelperRoleBase.ArtistModeratorAdminRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Work>]
    public IQueryable<Work> GetWorks()
    {
        return _workService.GetWorks();
    }

    [AuthorizeRoles(HelperRoleBase.ArtistModeratorAdminRoles)]
    [UseProjection]
    public async Task<WorkTempRequest> GetMetadataWorkUploadRequestAsync(string uploadId)
    {
        ICacheResult<CombinedUploadRequest> cacheResult = await _redisCacheService.TryGetGenericAsync<CombinedUploadRequest>($"upload:{uploadId}:requestUpload");
        if (!cacheResult.Success)
        {
            throw new NotFoundCustomException("Upload request not found or expired.");
        }

        return cacheResult.Value!.Work;
    }
}
