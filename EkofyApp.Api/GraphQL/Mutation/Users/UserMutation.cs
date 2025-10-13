using EkofyApp.Application.Models.UserFollows;
using EkofyApp.Application.Models.Users;
using EkofyApp.Application.ServiceInterfaces.Users;

namespace EkofyApp.Api.GraphQL.Mutation.Users;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class UserMutation(IUserService userService)
{
    private readonly IUserService _userService = userService;

    public async Task<bool> CreateModeratorAsync(CreateModeratorRequest createModeratorRequest)
    {
        await _userService.CreateModeratorAsync(createModeratorRequest);
        return true;
    }

    public async Task<bool> CreateAdminAsync(CreateAdminRequest createAdminRequest)
    {
        await _userService.CreateAdminAsync(createAdminRequest);
        return true;
    }

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

    public async Task<bool> BanUserAsync(string targetUserId)
    {
        await _userService.BanUserAsync(targetUserId);
        return true;
    }

    public async Task<bool> ReActiveUserAsync(string targetUserId)
    {
        await _userService.ReActiveUserAsync(targetUserId);
        return true;
    }
}
