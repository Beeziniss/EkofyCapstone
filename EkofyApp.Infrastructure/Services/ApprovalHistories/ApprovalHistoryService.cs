using EkofyApp.Application.Models.ApprovalHistories;
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

    public async Task CreateApprovalHistoryAsync(ApprovalHistoryRequest approvalHistoryRequest)
    {
        await unitOfWork.GetCollection<ApprovalHistory>().InsertOneAsync(new ApprovalHistory
        {
            TargetOwnerId = approvalHistoryRequest.TargetOwnerId,
            TargetId = approvalHistoryRequest.TargetId,
            ApprovalType = approvalHistoryRequest.ApprovalType,
            ApprovedByUserId = approvalHistoryRequest.ApprovedByUserId,
            ApprovedAt = approvalHistoryRequest.ApprovedAt,
            Action = approvalHistoryRequest.Action,
            Notes = approvalHistoryRequest.Notes,
            Snapshot = approvalHistoryRequest.Snapshot
        });
    }
}
