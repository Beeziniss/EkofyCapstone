using EkofyApp.Application.ServiceInterfaces.Revenues;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Mutation.Revenues;

[ExtendObjectType<MutationInitialization>]
[MutationType]
public sealed class PlatformRevenueMutation(IPlatformRevenueService platformRevenueService)
{
    private readonly IPlatformRevenueService _platformRevenueService = platformRevenueService;

    public async Task<PlatformRevenue> ComputePlatformRevenueAsync()
    {
        return await _platformRevenueService.ComputePlatformRevenueAsync();
    }
}
