using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver;
[ExtendObjectType(typeof(Recording))]
public sealed class RecordingResolver
{
    public async Task<Track?> GetTrackAsync(
        [Parent] Recording recording,
        DataLoaderCustomOneToOne<Track> trackByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await trackByIdDataLoader.LoadAsync(recording.TrackId, cancellationToken);
    }

    public async Task<IEnumerable<User?>> GetUsersAsync(
        [Parent] Recording recording,
        DataLoaderCustomOneToOne<User> userByIdDataLoader,
        CancellationToken cancellationToken)
    {
        List<string> userIds = recording.RecordingSplits.Select(rs => rs.UserId).ToList();
        return await userByIdDataLoader.LoadAsync(userIds, cancellationToken) ?? [];
    }
}
