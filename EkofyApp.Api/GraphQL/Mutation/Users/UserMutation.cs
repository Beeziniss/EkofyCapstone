using EkofyApp.Application.Models.UserEngagements;
using EkofyApp.Application.Models.Users;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Enums;

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

    public async Task<bool> FollowUserAsync(UserEngagementRequest request)
    {
        await _userService.FollowUserAsync(request);
        return true;
    }

    public async Task<bool> UnfollowUserAsync(UserEngagementRequest request)
    {
        await _userService.UnfollowUserAsync(request);
        return true;
    }

    public async Task<bool> BanUserAsync(string targetUserId)
    {
        await _userService.BanUserAsync(targetUserId);
        return true;
    }

    public async Task<bool> UnbanUserAsync(string targetUserId)
    {
        await _userService.UnbanUserAsync(targetUserId);
        return true;
    }

    public async Task<bool> DeleteUserManualAsync(string userId)
    {
        await _userService.DeleteUserManualAsync(userId);
        return true;
    }
}
