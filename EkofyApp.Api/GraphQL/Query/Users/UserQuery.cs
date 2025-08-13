using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Entities;
using HotChocolate.Authorization;

namespace EkofyApp.Api.GraphQL.Query.Users;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class UserQuery(IUserService userService)
{
    private readonly IUserService _userService = userService;

    public IQueryable<User> GetUsers()
    {
        return _userService.GetUsersQueryable();
    }

    // TODO: Query Object thì dùng Generic T ()
    // Vì IQueryable chỉ trả về format chính là List/IEnumerable
    public Task<User> GetUserByIdAsync(string id)
    {
        return _userService.GetUserByIdAsync(id);
    }
}
