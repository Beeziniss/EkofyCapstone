using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Listener))]
public sealed class ListenerResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUser([Parent] Listener listener, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => x.Id == listener.UserId);
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetFollowingsUser([Parent] Listener listener, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => listener.LastFollowings.Contains(x.Id));
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetFollowersUser([Parent] Listener listener, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => listener.LastFollowers.Contains(x.Id));
    }
}
