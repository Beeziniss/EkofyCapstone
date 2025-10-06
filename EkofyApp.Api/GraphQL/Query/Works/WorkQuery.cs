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

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Work>]
    public IQueryable<Work> GetWorksQueryable()
    {
        return _workService.GetWorksQueryable();
    }

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseProjection]
    public async Task<WorkTempRequest> GetMetadataWorkUploadRequestAsync(string workId)
    {
        ICacheResult<WorkTempRequest> cacheResult = await _redisCacheService.TryGetGenericAsync<WorkTempRequest>($"work:{workId}:requestUpload");
        if (!cacheResult.Success)
        {
            throw new NotFoundCustomException("WorkProjection upload request not found or expired.");
        }

        return cacheResult.Value!;
    }
}
