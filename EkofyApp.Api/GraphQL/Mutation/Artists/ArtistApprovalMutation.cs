using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Artists;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class ArtistApprovalMutation(IArtistService artistService)
{
    private readonly IArtistService _artistService = artistService;

    public async Task<bool> ApproveArtistRegistrationAsync(ArtistRegistrationApprovalRequest request)
    {
        await _artistService.ApproveArtistRegistrationAsync(request);
        return true;
    }

    public async Task<bool> RejectArtistRegistrationAsync(ArtistRegistrationApprovalRequest request)
    {
        await _artistService.RejectArtistRegistrationAsync(request);
        return true;
    }
}