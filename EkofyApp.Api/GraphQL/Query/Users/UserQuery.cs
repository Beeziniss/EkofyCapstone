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

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<User>]
    public IQueryable<User> GetUsers()
    {
        return _userService.GetUsers();
    }

    // TODO: Query Object thì dùng Generic T ()
    // Vì IQueryable chỉ trả về format chính là List/IEnumerable
    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseProjection]
    [UseSorting<User>]
    public Task<User> GetUserByIdAsync(string id)
    {
        return _userService.GetUserByIdAsync(id);
    }
}
