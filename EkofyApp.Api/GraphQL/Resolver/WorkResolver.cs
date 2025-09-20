using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Work))]
public sealed class WorkResolver
{
    public async Task<Track?> GetTrackAsync(
        [Parent] Work work,
        DataLoaderCustomOneToOne<Track> trackByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await trackByIdDataLoader.LoadAsync(work.TrackId, cancellationToken);
    }

    public async Task<IEnumerable<User?>> GetUsersAsync(
        [Parent] Work work,
        DataLoaderCustomOneToOne<User> userByIdDataLoader,
        CancellationToken cancellationToken)
    {
        List<string> userIds = work.WorkSplits.Select(ws => ws.UserId).ToList();
        return await userByIdDataLoader.LoadAsync(userIds, cancellationToken) ?? [];
    }
}
