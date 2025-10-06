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

    public async Task<bool> DeActiveUserAsync(string targetUserId)
    {
        await _userService.DeActiveUserAsync(targetUserId);
        return true;
    }

    public async Task<bool> ReActiveUserAsync(string targetUserId)
    {
        await _userService.ReActiveUserAsync(targetUserId);
        return true;
    }
}
