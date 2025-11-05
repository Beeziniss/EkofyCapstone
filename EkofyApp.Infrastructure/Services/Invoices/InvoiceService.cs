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

    public IQueryable<Invoice> GetInvoicesByUserId(string userId)
    {
        return _unitOfWork.GetCollection<Invoice>()
            .Find(x => x.UserId == userId)
            .ToEnumerable()
            .AsQueryable();
    }
}
