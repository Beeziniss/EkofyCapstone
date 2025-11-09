using EkofyApp.Application.ServiceInterfaces.Revenues;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Mutation.Revenues;

[ExtendObjectType<MutationInitialization>]
[MutationType]
public sealed class ArtistRevenueMutation(IArtistRevenueService artistRevenueService)
{
    private readonly IArtistRevenueService _artistRevenueService = artistRevenueService;

    public async Task<ArtistRevenue> ComputeArtistRevenueByArtistIdAsync(string artistId)
    {
        return await _artistRevenueService.ComputeArtistRevenueByArtistIdAsync(artistId);
    }
}
