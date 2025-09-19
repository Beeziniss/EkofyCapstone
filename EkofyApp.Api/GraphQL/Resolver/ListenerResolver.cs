using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Listener))]
public sealed class ListenerResolver
{
    public async Task<User?> GetUserAsync(
        [Parent] Listener listener,
        DataLoaderCustomOneToOne<User> userByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await userByIdDataLoader.LoadAsync(listener.UserId, cancellationToken);
    }

    // TODO: Cần test kỹ hàm này
    public async Task<IEnumerable<User>> GetFollowingUserAsync(
        [Parent] Listener listener,
        DataLoaderCustomOneToMany<User> userByIdDataLoader,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<IEnumerable<User>?> result = await userByIdDataLoader.LoadAsync(listener.LastFollowing, cancellationToken);
        // result is IReadOnlyList<IEnumerable<User>?>, flatten and filter nulls
        return result.Where(x => x != null).SelectMany(x => x!) ?? [];
    }
}
