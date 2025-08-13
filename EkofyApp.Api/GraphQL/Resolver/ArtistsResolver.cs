using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Artist))]
public sealed class ArtistsResolver
{
    public async Task<User> GetUserAsync(
        [Parent] Artist artist,
        DataLoaderCustomOneToOne<User> userByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await userByIdDataLoader.LoadAsync(artist.UserId, cancellationToken) ?? new User();
    }

    public async Task<IEnumerable<Category?>> GetCategoriesAsync(
        [Parent] Artist artist,
        DataLoaderCustomOneToOne<Category> categoriesDataLoader,
        CancellationToken cancellationToken)
    {
        return await categoriesDataLoader.LoadAsync(artist.CategoryIds, cancellationToken) ?? [];
    }
}
