using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.ServiceInterfaces.Artists;

namespace EkofyApp.Api.GraphQL.Mutation.Artists;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public class ArtistMutation(IArtistService artistService)
{
    private readonly IArtistService _artistService = artistService;

    public async Task<bool> CreateArtistAsync(CreateArtistRequest createArtistRequest)
    {
        return await _artistService.CreateArtistAsync(createArtistRequest);
    }
}
