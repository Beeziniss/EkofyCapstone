using EkofyApp.Application.Models.Works;
using EkofyApp.Application.ServiceInterfaces.Works;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;

namespace EkofyApp.Api.GraphQL.Query.Works;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class WorkQuery(IWorkService workService, IRedisCacheService redisCacheService)
{
    private readonly IWorkService _workService = workService;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;

    public IQueryable<Work> GetWorksQueryable()
    {
        return _workService.GetWorksQueryable();
    }

    public async Task<WorkTempRequest> GetMetadataWorkUploadRequestAsync(string workId)
    {
        ICacheResult<WorkTempRequest> cacheResult = await _redisCacheService.TryGetAsync<WorkTempRequest>($"work:{workId}:requestUpload");
        if (!cacheResult.Success)
        {
            throw new NotFoundCustomException("WorkProjection upload request not found or expired.");
        }

        return cacheResult.Value!;
    }
}
