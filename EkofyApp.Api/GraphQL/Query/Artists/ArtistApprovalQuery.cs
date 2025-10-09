using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Artists;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class ArtistApprovalQuery(IArtistService artistService)
{
    private readonly IArtistService _artistService = artistService;

    public async Task<IEnumerable<PendingArtistRegistrationResponse>> GetPendingArtistRegistrationsAsync(int pageNumber = 1, int pageSize = 20)
    {
        return await _artistService.GetPendingRegistrationsAsync(pageNumber, pageSize);
    }
}