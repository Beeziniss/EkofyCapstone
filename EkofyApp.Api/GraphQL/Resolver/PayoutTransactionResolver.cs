using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(PayoutTransaction))]
public sealed class PayoutTransactionResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUser([Parent] PayoutTransaction transaction, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(u => u.Id == transaction.UserId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Listener> GetListener([Parent] PayoutTransaction transaction, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Listener>().AsQueryable().Where(l => l.UserId == transaction.UserId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Artist> GetArtist([Parent] PayoutTransaction transaction, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Artist>().AsQueryable().Where(a => a.UserId == transaction.UserId);
    }
}
