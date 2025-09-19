using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.MonthlyStreamCounts;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.MonthlyStreamCounts;
public sealed class MonthlyStreamCountService(IUnitOfWork unitOfWork) : IMonthlyStreamCountService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<MonthlyStreamCount> GetMonthlyStreamCounts()
    {
        return _unitOfWork.GetCollection<MonthlyStreamCount>().AsQueryable();
    }
}
