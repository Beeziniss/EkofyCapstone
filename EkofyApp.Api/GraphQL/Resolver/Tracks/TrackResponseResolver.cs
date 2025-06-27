using AutoMapper;
using EkofyApp.Api.GraphQL.DataLoader.Artists;
using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver.Tracks;

[ExtendObjectType(typeof(TrackResponse))]
public class TrackResponseResolver
{
    public async Task<ArtistResponse?> GetArtistAsync(
        [Parent] TrackResponse trackResponse,
        ArtistByIdDataLoader artistByIdDataLoader,
        [Service] IMapper mapper,
        CancellationToken cancellationToken)
    {
        Artist? artist = await artistByIdDataLoader.LoadAsync(trackResponse.ArtistId, cancellationToken);
        
        return mapper.Map<ArtistResponse>(artist);
    }
}
