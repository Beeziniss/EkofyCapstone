using EkofyApp.Application.ServiceInterfaces.PackageOrders;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.PackageOrders;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class PackageOrderQuery(IPackageOrderService packageOrderService)
{
    private readonly IPackageOrderService _packageOrderService = packageOrderService;

    [AuthorizeRoles(HelperRoleBase.ListenerArtistModeratorRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<PackageOrder>]
    public IQueryable<PackageOrder> GetPackageOrders()
    {
        return _packageOrderService.GetPackageOrders();
    }
}
