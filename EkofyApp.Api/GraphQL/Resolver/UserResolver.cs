using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(User))]
public sealed class UserResolver
{
    public async Task<bool> CheckUserFollowingAsync([Parent] User user, [Service] IUserService userService)
    {
        return await userService.CheckUserFollowingAsync(user.Id);
    }
}
