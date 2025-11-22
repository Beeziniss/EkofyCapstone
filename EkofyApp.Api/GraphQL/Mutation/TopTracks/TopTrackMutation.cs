using EkofyApp.Application.ServiceInterfaces.TopTracks;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.TopTracks;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public class TopTrackMutation(ITopTrackService topTrackService)
{
    private readonly ITopTrackService _topTrackService = topTrackService;

    [AuthorizeRoles(HelperRoleBase.ListenerRoles)]
    public async Task<bool> UpsertTopTrackCountAsync(string trackId)
    {
        await _topTrackService.UpsertTopTrackCountAsync(trackId);
        return true;
    }
}
