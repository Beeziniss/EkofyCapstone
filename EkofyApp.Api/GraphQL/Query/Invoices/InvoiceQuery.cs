using EkofyApp.Application.ServiceInterfaces.Invoices;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Invoices;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class InvoiceQuery(IInvoiceService invoiceService)
{
    private readonly IInvoiceService _invoiceService = invoiceService;
    public IQueryable<Invoice> GetInvoices()
    {
        return _invoiceService.GetInvoices();
    }
}
