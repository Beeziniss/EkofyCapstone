using EkofyApp.Application.ServiceInterfaces.Invoices;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Invoices;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class InvoiceQuery(IInvoiceService invoiceService)
{
    private readonly IInvoiceService _invoiceService = invoiceService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Invoice>]
    public IQueryable<Invoice> GetInvoices()
    {
        return _invoiceService.GetInvoices();
    }
}
