using AutoMapper;
using EkofyApp.Api.GraphQL.DataLoader.Artist;
using EkofyApp.Application.Models.Artist;
using EkofyApp.Application.Models.Track;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver.Track;

[ExtendObjectType(typeof(TrackResponse))]
public class TrackResponseResolver
{
    public async Task<ArtistResponse?> GetArtistAsync(
        [Parent] TrackResponse trackResponse,
        ArtistByIdDataLoader artistByIdDataLoader,
        [Service] IMapper mapper,
        CancellationToken cancellationToken)
    {
        Artists? artist = await artistByIdDataLoader.LoadAsync(trackResponse.ArtistId, cancellationToken);
        
        return mapper.Map<ArtistResponse>(artist);
    }
}
