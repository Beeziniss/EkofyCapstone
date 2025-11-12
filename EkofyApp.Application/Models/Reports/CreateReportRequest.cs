using EkofyApp.Domain.Enums.Reports;

namespace EkofyApp.Application.Models.Reports;

/// <summary>
/// Requests ?? t?o báo cáo vi ph?m
/// </summary>
public sealed class CreateReportRequest
{
    /// <summary>
    /// User b? báo cáo
    /// </summary>
    public string ReportedUserId { get; set; } = null!;

    /// <summary>
    /// Lo?i vi ph?m
    /// </summary>
    public ReportType ReportType { get; set; }

    /// <summary>
    /// Mô t? chi ti?t
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// ID n?i dung vi ph?m (optional)
    /// </summary>
    public string? RelatedContentId { get; set; }

    /// <summary>
    /// Lo?i n?i dung (Track, Comment, Playlist, Profile, etc.)
    /// </summary>
    public ReportRelatedContentType? RelatedContentType { get; set; }

    /// <summary>
    /// URL b?ng ch?ng
    /// </summary>
    public List<string>? Evidences { get; set; }
}
