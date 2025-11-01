using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Authorization;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Users;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class UserQuery(IUserService userService)
{
    private readonly IUserService _userService = userService;

    //[AuthorizeRoles(HelperRoleBase.FullRoles)]
    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<User>]
    public IQueryable<User> GetUsers()
    {
        return _userService.GetUsers();
    }

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<User>]
    public IQueryable<User> GetFollowersByUserId(string userId)
    {
        return _userService.GetFollowersByUserId(userId);
    }

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<User>]
    public IQueryable<User> GetFollowingsByUserId(string userId)
    {
        return _userService.GetFollowingsByUserId(userId);
    }
}
