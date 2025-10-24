using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Entities;
using HotChocolate.Authorization;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Users;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class UserEngagementQuery(IUserService userService)
{
    private readonly IUserService _userService = userService;

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<UserEngagement>]
    public IQueryable<UserEngagement> GetUserEngagement()
    {
        return _userService.GetUserEngagement();
    }
}
