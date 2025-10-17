using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(PaymentTransaction))]
public sealed class PaymentTransactionResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUser([Parent] PaymentTransaction transaction, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(u => u.Id == transaction.UserId);
    }
}
