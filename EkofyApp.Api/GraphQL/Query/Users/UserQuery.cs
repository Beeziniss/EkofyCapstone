using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
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
    public IQueryable<User> GetFollowers(string? userId, string? artistId)
    {
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(artistId))
        {
            throw new BadRequestCustomException("Only userId or artistId should be passed, not both.");
        }

        if (userId != null)
        {
            return _userService.GetFollowersByUserId(userId);
        }
        else
        {
            return _userService.GetFollowersByArtistId(artistId!);
        }
    }

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<User>]
    public IQueryable<User> GetFollowings(string? userId, string? artistId)
    {
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(artistId))
        {
            throw new BadRequestCustomException("Only userId or artistId should be passed, not both.");
        }

        if (userId != null)
        {
            return _userService.GetFollowingsByUserId(userId);
        }
        else
        {
            return _userService.GetFollowingsByArtistId(artistId!);
        }
    }

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<User>]
    public IQueryable<PaymentTransaction> GetPaymentTransactionsByUserId(string userId)
    {
        return _userService.GetPaymentTransactionsByUserId(userId);
    }
}
