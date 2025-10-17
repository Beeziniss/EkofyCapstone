using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.ApprovalHistories;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.ApprovalHistories;
public sealed class ApprovalHistoryService(IUnitOfWork unitOfWork) : IApprovalHistoryService
{
    private readonly IUnitOfWork unitOfWork = unitOfWork;

    public IQueryable<ApprovalHistory> GetApprovalHistories()
    {
        return unitOfWork.GetCollection<ApprovalHistory>().AsQueryable();
    }
}
