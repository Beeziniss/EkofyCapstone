using EkofyApp.Application.Models.Artist;
using EkofyApp.Application.ServiceInterfaces.Artist;

namespace EkofyApp.Api.GraphQL.Mutation.Artist;

[ExtendObjectType(typeof(MutationInitialization))]
public class ArtistMutation(IArtistService artistService)
{
    private readonly IArtistService _artistService = artistService;

    public async Task<bool> CreateArtistAsync(CreateArtistRequest createArtistRequest)
    {
        return await _artistService.CreateArtistAsync(createArtistRequest);
    }
}
