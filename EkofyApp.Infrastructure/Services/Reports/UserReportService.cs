using AutoMapper;
using EkofyApp.Application.Models.Reports;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Reports;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Reports;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Reports;

public sealed class UserReportService(
    IUnitOfWork unitOfWork,
    IHttpContextAccessor httpContextAccessor,
    IMapper mapper) : IUserReportService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IMapper _mapper = mapper;

    private string GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedCustomException("Your session is limited");
    }

    private string GetCurrentUserRole()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirst("role")?.Value
            ?? throw new UnauthorizedCustomException("Your session is limited");
    }

    /// <summary>
    /// T?o báo cáo vi ph?m m?i
    /// </summary>
    public async Task<ReportResponse> CreateReportAsync(CreateReportRequest request)
    {
        string reporterId = GetCurrentUserId();

        // Ki?m tra user b? báo cáo có t?n t?i không
        User? reportedUser = await _unitOfWork.GetCollection<User>()
            .Find(u => u.Id == request.ReportedUserId && u.Status != UserStatus.Banned)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundCustomException("Reported user not found");

        // Không cho phép t? báo cáo chính mình
        if (reporterId == request.ReportedUserId)
        {
            throw new BadRequestCustomException("You cannot report yourself");
        }

        // Ki?m tra xem ?ã báo cáo user này ch?a (trong vòng 24h)
        DateTimeOffset last24Hours = DateTimeOffset.UtcNow.AddHours(-24);
        bool existingReport = await _unitOfWork.GetCollection<UserReport>()
            .Find(r => r.ReporterId == reporterId 
                && r.ReportedUserId == request.ReportedUserId
                && r.CreatedAt >= last24Hours
                && r.Status == ReportStatus.Pending)
            .AnyAsync();

        if (existingReport)
        {
            throw new BadRequestCustomException("You have already reported this user in the last 24 hours");
        }

        // ??m s? l?n user này b? báo cáo
        long totalReportsCount = await _unitOfWork.GetCollection<UserReport>()
            .CountDocumentsAsync(r => r.ReportedUserId == request.ReportedUserId 
                && r.Status != ReportStatus.Rejected
                && r.Status != ReportStatus.Dismissed);

        // T? ??ng t?ng priority n?u user b? báo cáo nhi?u
        ReportPriority priority = totalReportsCount switch
        {
            >= 10 => ReportPriority.Critical,
            >= 5 => ReportPriority.High,
            >= 2 => ReportPriority.Medium,
            _ => ReportPriority.Low
        };

        UserReport report = new()
        {
            ReportedUserId = request.ReportedUserId,
            ReporterId = reporterId,
            ReportType = request.ReportType,
            Description = request.Description,
            Status = ReportStatus.Pending,
            Priority = priority,
            RelatedContentId = request.RelatedContentId,
            RelatedContentType = request.RelatedContentType,
            EvidenceUrls = request.EvidenceUrls ?? [],
            TotalReportsCount = (int)totalReportsCount + 1,
            CreatedBy = reporterId
        };

        await _unitOfWork.GetCollection<UserReport>().InsertOneAsync(report);

        return await GetReportByIdAsync(report.Id);
    }

    /// <summary>
    /// L?y danh sách báo cáo (v?i filter và pagination)
    /// </summary>
    public async Task<ReportListResponse> GetReportsAsync(GetReportsRequest request)
    {
        // Ch? moderator và admin m?i xem ???c
        string role = GetCurrentUserRole();
        if (role != UserRole.Moderator.ToString() && role != UserRole.Admin.ToString())
        {
            throw new ForbiddenCustomException("You don't have permission to view reports");
        }

        FilterDefinitionBuilder<UserReport> filterBuilder = Builders<UserReport>.Filter;
        FilterDefinition<UserReport> filter = filterBuilder.Eq(r => r.IsDeleted, false);

        // Apply filters
        if (request.Status.HasValue)
        {
            filter &= filterBuilder.Eq(r => r.Status, request.Status.Value);
        }

        if (request.ReportType.HasValue)
        {
            filter &= filterBuilder.Eq(r => r.ReportType, request.ReportType.Value);
        }

        if (request.Priority.HasValue)
        {
            filter &= filterBuilder.Eq(r => r.Priority, request.Priority.Value);
        }

        if (!string.IsNullOrEmpty(request.ReportedUserId))
        {
            filter &= filterBuilder.Eq(r => r.ReportedUserId, request.ReportedUserId);
        }

        if (!string.IsNullOrEmpty(request.AssignedModeratorId))
        {
            filter &= filterBuilder.Eq(r => r.AssignedModeratorId, request.AssignedModeratorId);
        }

        if (request.FromDate.HasValue)
        {
            filter &= filterBuilder.Gte(r => r.CreatedAt, request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            filter &= filterBuilder.Lte(r => r.CreatedAt, request.ToDate.Value);
        }

        // Count total
        long totalCount = await _unitOfWork.GetCollection<UserReport>()
            .CountDocumentsAsync(filter);

        // Get paginated data
        List<UserReport> reports = await _unitOfWork.GetCollection<UserReport>()
            .Find(filter)
            .SortByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Limit(request.PageSize)
            .ToListAsync();

        // Map to response
        List<ReportResponse> reportResponses = await MapReportsToResponsesAsync(reports);

        int totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new ReportListResponse
        {
            Reports = reportResponses,
            TotalCount = (int)totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasNextPage = request.PageNumber < totalPages,
            HasPreviousPage = request.PageNumber > 1
        };
    }

    /// <summary>
    /// L?y chi ti?t m?t báo cáo
    /// </summary>
    public async Task<ReportResponse> GetReportByIdAsync(string reportId)
    {
        UserReport report = await _unitOfWork.GetCollection<UserReport>()
            .Find(r => r.Id == reportId && r.IsDeleted == false)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundCustomException("Report not found");

        return await MapReportToResponseAsync(report);
    }

    /// <summary>
    /// Assign báo cáo cho moderator
    /// </summary>
    public async Task<bool> AssignReportToModeratorAsync(string reportId, string moderatorId)
    {
        string currentUserId = GetCurrentUserId();
        string role = GetCurrentUserRole();

        // Ch? admin ho?c moderator t? assign
        if (role != UserRole.Admin.ToString() && currentUserId != moderatorId)
        {
            throw new ForbiddenCustomException("Only admin can assign reports to other moderators");
        }

        // Ki?m tra moderator có t?n t?i không
        User? moderator = await _unitOfWork.GetCollection<User>()
            .Find(u => u.Id == moderatorId && u.Role == UserRole.Moderator)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundCustomException("Moderator not found");

        UpdateResult result = await _unitOfWork.GetCollection<UserReport>()
            .UpdateOneAsync(
                r => r.Id == reportId && r.IsDeleted == false,
                Builders<UserReport>.Update
                    .Set(r => r.AssignedModeratorId, moderatorId)
                    .Set(r => r.Status, ReportStatus.UnderReview)
                    .Set(r => r.UpdatedAt, DateTimeOffset.UtcNow)
                    .Set(r => r.UpdatedBy, currentUserId)
            );

        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Moderator x? lý báo cáo
    /// </summary>
    public async Task<ReportResponse> ProcessReportAsync(ProcessReportRequest request)
    {
        string moderatorId = GetCurrentUserId();
        string role = GetCurrentUserRole();

        // Ch? moderator và admin m?i x? lý ???c
        if (role != UserRole.Moderator.ToString() && role != UserRole.Admin.ToString())
        {
            throw new ForbiddenCustomException("You don't have permission to process reports");
        }

        UserReport report = await _unitOfWork.GetCollection<UserReport>()
            .Find(r => r.Id == request.ReportId && r.IsDeleted == false)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundCustomException("Report not found");

        // B?t ??u transaction
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // Update report
            UpdateResult updateResult = await _unitOfWork.GetCollection<UserReport>()
                .UpdateOneAsync(
                    r => r.Id == request.ReportId,
                    Builders<UserReport>.Update
                        .Set(r => r.Status, request.Status)
                        .Set(r => r.ActionTaken, request.ActionTaken)
                        .Set(r => r.ModeratorNotes, request.ModeratorNotes)
                        .Set(r => r.ReviewedAt, DateTimeOffset.UtcNow)
                        .Set(r => r.ResolvedAt, DateTimeOffset.UtcNow)
                        .Set(r => r.AssignedModeratorId, moderatorId)
                        .Set(r => r.UpdatedAt, DateTimeOffset.UtcNow)
                        .Set(r => r.UpdatedBy, moderatorId)
                );

            if (updateResult.ModifiedCount == 0)
            {
                throw new BadRequestCustomException("Failed to update report");
            }

            // Th?c hi?n hành ??ng v?i user n?u báo cáo ???c approve
            if (request.Status == ReportStatus.Approved && request.ActionTaken != ReportAction.NoAction)
            {
                await ApplyActionToUserAsync(report.ReportedUserId, request, moderatorId, report.Id);
            }

            await _unitOfWork.CommitAsync();

            return await GetReportByIdAsync(request.ReportId);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Th?c hi?n hành ??ng x? ph?t user
    /// </summary>
    private async Task ApplyActionToUserAsync(string userId, ProcessReportRequest request, string moderatorId, string reportId)
    {
        switch (request.ActionTaken)
        {
            case ReportAction.Warning:
                // Ch? ghi log, không thay ??i status user
                break;

            case ReportAction.TemporarySuspension:
                if (!request.SuspensionDays.HasValue || request.SuspensionDays <= 0)
                {
                    throw new BadRequestCustomException("Suspension days must be greater than 0");
                }

                // Update user status
                await _unitOfWork.GetCollection<User>()
                    .UpdateOneAsync(
                        u => u.Id == userId,
                        Builders<User>.Update
                            .Set(u => u.Status, UserStatus.Suspended)
                            .Set(u => u.UpdatedAt, DateTimeOffset.UtcNow)
                    );

                // T?o restriction record
                UserRestriction restriction = new()
                {
                    UserId = userId,
                    RestrictionType = RestrictionType.Suspended,
                    Reason = request.ModeratorNotes ?? "Temporary suspension due to policy violation",
                    ReportId = reportId,
                    ModeratorId = moderatorId,
                    ActionType = ReportAction.TemporarySuspension,
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = DateTimeOffset.UtcNow.AddDays(request.SuspensionDays.Value),
                    DurationDays = request.SuspensionDays.Value,
                    IsActive = true,
                    Notes = request.ModeratorNotes
                };

                await _unitOfWork.GetCollection<UserRestriction>().InsertOneAsync(restriction);
                break;

            case ReportAction.PermanentBan:
                // Update user status
                await _unitOfWork.GetCollection<User>()
                    .UpdateOneAsync(
                        u => u.Id == userId,
                        Builders<User>.Update
                            .Set(u => u.Status, UserStatus.Banned)
                            .Set(u => u.UpdatedAt, DateTimeOffset.UtcNow)
                    );

                // T?o restriction record
                UserRestriction banRestriction = new()
                {
                    UserId = userId,
                    RestrictionType = RestrictionType.Banned,
                    Reason = request.ModeratorNotes ?? "Permanent ban due to severe policy violation",
                    ReportId = reportId,
                    ModeratorId = moderatorId,
                    ActionType = ReportAction.PermanentBan,
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = null, // Permanent
                    IsActive = true,
                    Notes = request.ModeratorNotes
                };

                await _unitOfWork.GetCollection<UserRestriction>().InsertOneAsync(banRestriction);
                break;

            case ReportAction.AccountRestriction:
                // T?o restriction record v?i các h?n ch? c? th?
                UserRestriction accountRestriction = new()
                {
                    UserId = userId,
                    RestrictionType = RestrictionType.Suspended,
                    Reason = request.ModeratorNotes ?? "Account restriction due to policy violation",
                    ReportId = reportId,
                    ModeratorId = moderatorId,
                    ActionType = ReportAction.AccountRestriction,
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = request.SuspensionDays.HasValue 
                        ? DateTimeOffset.UtcNow.AddDays(request.SuspensionDays.Value) 
                        : null,
                    DurationDays = request.SuspensionDays,
                    IsActive = true,
                    Notes = request.ModeratorNotes
                };

                await _unitOfWork.GetCollection<UserRestriction>().InsertOneAsync(accountRestriction);
                break;

            case ReportAction.ContentRemoval:
                // Logic xóa content n?u có RelatedContentId
                // TODO: Implement based on content type
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// L?y t?t c? báo cáo v? m?t user
    /// </summary>
    public async Task<List<ReportResponse>> GetReportsByUserIdAsync(string userId)
    {
        List<UserReport> reports = await _unitOfWork.GetCollection<UserReport>()
            .Find(r => r.ReportedUserId == userId && r.IsDeleted == false)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync();

        return await MapReportsToResponsesAsync(reports);
    }

    /// <summary>
    /// L?y statistics v? reports
    /// </summary>
    public async Task<ReportStatisticsResponse> GetReportStatisticsAsync()
    {
        string role = GetCurrentUserRole();
        if (role != UserRole.Moderator.ToString() && role != UserRole.Admin.ToString())
        {
            throw new ForbiddenCustomException("You don't have permission to view statistics");
        }

        IMongoCollection<UserReport> collection = _unitOfWork.GetCollection<UserReport>();

        int totalReports = (int)await collection.CountDocumentsAsync(r => r.IsDeleted == false);
        int pendingReports = (int)await collection.CountDocumentsAsync(r => r.Status == ReportStatus.Pending && r.IsDeleted == false);
        int underReviewReports = (int)await collection.CountDocumentsAsync(r => r.Status == ReportStatus.UnderReview && r.IsDeleted == false);
        int resolvedReports = (int)await collection.CountDocumentsAsync(r => r.Status == ReportStatus.Approved && r.IsDeleted == false);
        int rejectedReports = (int)await collection.CountDocumentsAsync(r => r.Status == ReportStatus.Rejected && r.IsDeleted == false);

        // Group by type
        var reportsByType = await collection.Aggregate()
            .Match(r => r.IsDeleted == false)
            .Group(r => r.ReportType, g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        // Group by priority
        var reportsByPriority = await collection.Aggregate()
            .Match(r => r.IsDeleted == false)
            .Group(r => r.Priority, g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync();

        // Top reported users
        var topReportedUsers = await collection.Aggregate()
            .Match(r => r.IsDeleted == false && r.Status != ReportStatus.Rejected)
            .Group(r => r.ReportedUserId, g => new { UserId = g.Key, Count = g.Count() })
            .SortByDescending(x => x.Count)
            .Limit(10)
            .ToListAsync();

        // Get user names
        List<TopReportedUserResponse> topReportedUserResponses = [];
        foreach (var item in topReportedUsers)
        {
            User? user = await _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == item.UserId)
                .Project(u => new User { Id = u.Id, FullName = u.FullName })
                .FirstOrDefaultAsync();

            if (user != null)
            {
                topReportedUserResponses.Add(new TopReportedUserResponse
                {
                    UserId = user.Id,
                    UserName = user.FullName,
                    ReportCount = item.Count
                });
            }
        }

        return new ReportStatisticsResponse
        {
            TotalReports = totalReports,
            PendingReports = pendingReports,
            UnderReviewReports = underReviewReports,
            ResolvedReports = resolvedReports,
            RejectedReports = rejectedReports,
            ReportsByType = reportsByType.ToDictionary(x => x.Type.ToString(), x => x.Count),
            ReportsByPriority = reportsByPriority.ToDictionary(x => x.Priority.ToString(), x => x.Count),
            TopReportedUsers = topReportedUserResponses
        };
    }

    /// <summary>
    /// Update priority c?a báo cáo
    /// </summary>
    public async Task<bool> UpdateReportPriorityAsync(string reportId, string priority)
    {
        string currentUserId = GetCurrentUserId();
        string role = GetCurrentUserRole();

        if (role != UserRole.Moderator.ToString() && role != UserRole.Admin.ToString())
        {
            throw new ForbiddenCustomException("You don't have permission to update report priority");
        }

        if (!Enum.TryParse<ReportPriority>(priority, true, out ReportPriority reportPriority))
        {
            throw new BadRequestCustomException("Invalid priority value");
        }

        UpdateResult result = await _unitOfWork.GetCollection<UserReport>()
            .UpdateOneAsync(
                r => r.Id == reportId && r.IsDeleted == false,
                Builders<UserReport>.Update
                    .Set(r => r.Priority, reportPriority)
                    .Set(r => r.UpdatedAt, DateTimeOffset.UtcNow)
                    .Set(r => r.UpdatedBy, currentUserId)
            );

        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Xóa báo cáo (soft delete)
    /// </summary>
    public async Task<bool> DeleteReportAsync(string reportId)
    {
        string currentUserId = GetCurrentUserId();
        string role = GetCurrentUserRole();

        if (role != UserRole.Admin.ToString())
        {
            throw new ForbiddenCustomException("Only admin can delete reports");
        }

        UpdateResult result = await _unitOfWork.GetCollection<UserReport>()
            .UpdateOneAsync(
                r => r.Id == reportId,
                Builders<UserReport>.Update
                    .Set(r => r.IsDeleted, true)
                    .Set(r => r.UpdatedAt, DateTimeOffset.UtcNow)
                    .Set(r => r.UpdatedBy, currentUserId)
            );

        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Escalate báo cáo lên admin
    /// </summary>
    public async Task<bool> EscalateReportAsync(string reportId)
    {
        string currentUserId = GetCurrentUserId();

        UpdateResult result = await _unitOfWork.GetCollection<UserReport>()
            .UpdateOneAsync(
                r => r.Id == reportId && r.IsDeleted == false,
                Builders<UserReport>.Update
                    .Set(r => r.Status, ReportStatus.Escalated)
                    .Set(r => r.Priority, ReportPriority.Critical)
                    .Set(r => r.UpdatedAt, DateTimeOffset.UtcNow)
                    .Set(r => r.UpdatedBy, currentUserId)
            );

        return result.ModifiedCount > 0;
    }

    #region Helper Methods

    private async Task<ReportResponse> MapReportToResponseAsync(UserReport report)
    {
        // Get reporter info
        User? reporter = await _unitOfWork.GetCollection<User>()
            .Find(u => u.Id == report.ReporterId)
            .Project(u => new User { Id = u.Id, FullName = u.FullName })
            .FirstOrDefaultAsync();

        // Get reported user info
        User? reportedUser = await _unitOfWork.GetCollection<User>()
            .Find(u => u.Id == report.ReportedUserId)
            .Project(u => new User { Id = u.Id, FullName = u.FullName })
            .FirstOrDefaultAsync();

        // Get moderator info if assigned
        User? moderator = null;
        if (!string.IsNullOrEmpty(report.AssignedModeratorId))
        {
            moderator = await _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == report.AssignedModeratorId)
                .Project(u => new User { Id = u.Id, FullName = u.FullName })
                .FirstOrDefaultAsync();
        }

        return new ReportResponse
        {
            Id = report.Id,
            ReportedUserId = report.ReportedUserId,
            ReportedUserName = reportedUser?.FullName ?? "Unknown",
            ReporterId = report.ReporterId,
            ReporterName = reporter?.FullName ?? "Unknown",
            ReportType = report.ReportType,
            Description = report.Description,
            Status = report.Status,
            Priority = report.Priority,
            RelatedContentId = report.RelatedContentId,
            RelatedContentType = report.RelatedContentType,
            EvidenceUrls = report.EvidenceUrls,
            AssignedModeratorId = report.AssignedModeratorId,
            AssignedModeratorName = moderator?.FullName,
            ReviewedAt = report.ReviewedAt,
            ActionTaken = report.ActionTaken,
            ModeratorNotes = report.ModeratorNotes,
            ResolvedAt = report.ResolvedAt,
            TotalReportsCount = report.TotalReportsCount,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    private async Task<List<ReportResponse>> MapReportsToResponsesAsync(List<UserReport> reports)
    {
        List<ReportResponse> responses = [];

        foreach (UserReport report in reports)
        {
            responses.Add(await MapReportToResponseAsync(report));
        }

        return responses;
    }

    #endregion
}
