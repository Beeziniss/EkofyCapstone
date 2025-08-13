using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Entities;

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
}
