using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Api.GraphQL.DataLoader.Artists;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Track))]
public sealed class TracksResolver
{
    public async Task<IEnumerable<Artist?>> GetArtistAsync(
        [Parent] Track track,
        //ArtistDataLoader artistByIdDataLoader,
        DataLoaderCustomOneToOne<Artist> artistByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await artistByIdDataLoader.LoadAsync(track.ArtistId, cancellationToken) ?? [];
    }

    public async Task<IEnumerable<Category?>> GetCategoryAsync(
        [Parent] Track track,
        DataLoaderCustomOneToOne<Category> categoryDataLoader,
        CancellationToken cancellationToken)
    {
        return await categoryDataLoader.LoadAsync(track.CategoryIds, cancellationToken) ?? [];
    }
}
