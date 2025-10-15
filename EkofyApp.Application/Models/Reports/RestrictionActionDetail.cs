using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Reports;
public sealed record class RestrictionActionDetail
{
    /// <summary>
    /// Danh sách các hành động cụ thể bị cấm.
    /// Chỉ áp dụng khi ActionTaken là EntitlementRestriction.
    /// </summary>
    public RestrictionAction RestrictionAction { get; init; }

    /// <summary>
    /// Ghi chú c?a moderator
    /// </summary>
    public string? Note { get; init; }
}
