using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Query.Payment;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class PayoutQuery(IUnitOfWork unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// Lấy danh sách payout transactions (Admin và Artist chỉ xem của mình)
    /// </summary>
    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<PayoutTransaction> GetPayoutTransactions([Service] IHttpContextAccessor httpContextAccessor)
    {
        string? userRole = httpContextAccessor.HttpContext?.User.FindFirst("role")?.Value;
        string? userId = httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;

        IQueryable<PayoutTransaction> query = _unitOfWork.GetCollection<PayoutTransaction>().AsQueryable();

        // Artist chỉ xem payout của mình, Admin xem tất cả
        if (userRole != "Admin")
        {
            query = query.Where(p => p.UserId == userId);
        }

        return query;
    }
}