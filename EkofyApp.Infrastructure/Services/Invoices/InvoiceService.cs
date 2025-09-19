using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Invoices;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Invoices;
public sealed class InvoiceService(IUnitOfWork unitOfWork) : IInvoiceService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<Invoice> GetInvoices()
    {
        return _unitOfWork.GetCollection<Invoice>().AsQueryable();
    }
}
