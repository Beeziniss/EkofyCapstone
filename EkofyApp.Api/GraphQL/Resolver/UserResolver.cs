using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(User))]
public sealed class UserResolver
{
    public async Task<bool> CheckUserFollowingAsync([Parent] User user, [Service] IUserService userService)
    {
        return await userService.CheckUserFollowingAsync(user.Id);
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Listener>]
    public IQueryable<Listener> GetListeners([Parent] User user, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Listener>().AsQueryable().Where(listener => listener.UserId == user.Id);
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Artist>]
    public IQueryable<Artist> GetArtists([Parent] User user, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Artist>().AsQueryable().Where(artist => artist.UserId == user.Id);
    }
}
