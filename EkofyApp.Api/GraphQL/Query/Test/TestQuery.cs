using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Query.Test;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class TestQuery
{
    public IQueryable<Entitlement> GetEntitlements([Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Entitlement>().AsQueryable();
    }
}
