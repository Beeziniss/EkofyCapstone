using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Invoice))]
public sealed class InvoiceResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUser([Parent] Invoice invoice, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => x.Id == invoice.UserId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<PaymentTransaction> GetTransaction([Parent] Invoice invoice, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<PaymentTransaction>().AsQueryable().Where(x => x.Id == invoice.PaymentTransactionId);
    }
}
