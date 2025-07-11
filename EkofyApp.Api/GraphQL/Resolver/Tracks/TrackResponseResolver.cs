using AutoMapper;
using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Api.GraphQL.DataLoader.Artists;
using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver.Tracks;

[ExtendObjectType(typeof(TrackResponse))]
public sealed class TrackResponseResolver
{
    public async Task<ArtistResponse?> GetArtistAsync(
       [Parent] TrackResponse trackResponse,
       ArtistDataLoader artistDataLoader,
       //DataLoaderCustom<Artist> artistDataLoader,
       [Service] IMapper mapper,
       CancellationToken cancellationToken)
    {
        //Artist? artist = await artistDataLoader.LoadAsync(trackResponse.ArtistId, cancellationToken);
        Artist? artist = await artistDataLoader.LoadAsync(trackResponse.ArtistId, cancellationToken);

        return mapper.Map<ArtistResponse>(artist);
    }
}
