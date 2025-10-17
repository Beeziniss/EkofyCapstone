using EkofyApp.Application.Models.ApprovalHistories;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.ApprovalHistories;
public interface IApprovalHistoryService
{
    Task CreateApprovalHistoryAsync(ApprovalHistoryRequest approvalHistoryRequest);
    IQueryable<ApprovalHistory> GetApprovalHistories();
}
