using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Invoices;
public interface IInvoiceService
{
    IQueryable<Invoice> GetInvoices();
    IQueryable<Invoice> GetInvoicesByUserId(string userId);
}
