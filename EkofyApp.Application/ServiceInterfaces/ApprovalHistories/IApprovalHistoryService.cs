using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.ApprovalHistories;
public interface IApprovalHistoryService
{
    IQueryable<ApprovalHistory> GetApprovalHistories();
}
