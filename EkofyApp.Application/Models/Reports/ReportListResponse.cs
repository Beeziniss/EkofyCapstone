namespace EkofyApp.Application.Models.Reports;

/// <summary>
/// Response cho danh sách báo cáo (paginated)
/// </summary>
public sealed class ReportListResponse
{
    public List<ReportResponse> Reports { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
