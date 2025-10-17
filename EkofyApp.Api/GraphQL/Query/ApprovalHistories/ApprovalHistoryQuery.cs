using EkofyApp.Application.ServiceInterfaces.ApprovalHistories;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.ApprovalHistories;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class ApprovalHistoryQuery(IApprovalHistoryService approvalHistoryService)
{
    private readonly IApprovalHistoryService _approvalHistoryService = approvalHistoryService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<ApprovalHistory>]
    public IQueryable<ApprovalHistory> GetApprovalHistories()
    {
        return _approvalHistoryService.GetApprovalHistories();
    }
}
