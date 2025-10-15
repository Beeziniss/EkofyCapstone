namespace EkofyApp.Application.Models.Reports;

/// <summary>
/// Statistics v? reports
/// </summary>
public sealed class ReportStatisticsResponse
{
    public int TotalReports { get; set; }
    public int PendingReports { get; set; }
    public int UnderReviewReports { get; set; }
    public int ResolvedReports { get; set; }
    public int RejectedReports { get; set; }
    public Dictionary<string, int> ReportsByType { get; set; } = [];
    public Dictionary<string, int> ReportsByPriority { get; set; } = [];
    public List<TopReportedUserResponse> TopReportedUsers { get; set; } = [];
}

public sealed class TopReportedUserResponse
{
    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public int ReportCount { get; set; }
}
