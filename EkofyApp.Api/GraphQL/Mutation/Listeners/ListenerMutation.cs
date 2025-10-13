using EkofyApp.Application.Models.Listeners;
using EkofyApp.Application.Models.UserFollows;
using EkofyApp.Application.ServiceInterfaces.Listeners;
using EkofyApp.Application.ServiceInterfaces.Users;

namespace EkofyApp.Api.GraphQL.Mutation.Listeners;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class ListenerMutation(IListenerService listenerService, IUserService userService)
{
    private readonly IListenerService _listenerService = listenerService;
    private readonly IUserService _userService = userService;

    public async Task<bool> UpdateProfileAsync(UpdateListenerRequest updateListenerRequest)
    {
        await _listenerService.UpdateProfileAsync(updateListenerRequest);
        return true;
    }

    #region Follow Methods

    public async Task<bool> FollowUserAsync(FollowUserRequest request)
    {
        await _userService.FollowUserAsync(request);
        return true;
    }

    public async Task<bool> UnfollowUserAsync(UnfollowUserRequest request)
    {
        await _userService.UnfollowUserAsync(request);
        return true;
    }

    #endregion
}
