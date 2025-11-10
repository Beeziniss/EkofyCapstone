using AutoMapper;
using EkofyApp.Application.Models.Reports;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Jobs;
using EkofyApp.Application.ServiceInterfaces.Reports;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Reports;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Hangfire;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Reports;

public sealed class ReportService(
    IUnitOfWork unitOfWork,
    IHttpContextAccessor httpContextAccessor,
    IMapper mapper) : IReportService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IMapper _mapper = mapper;

    public IQueryable<Report> GetReports()
    {
        return _unitOfWork.GetCollection<Report>().AsQueryable();
    }

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

    public async Task CreateReportAsync(CreateReportRequest request)
    {
        string reporterId = GetCurrentUserId();

        // Kiểm tra user bị reported có tồn tại không
        bool isReportedUserExisted = await _unitOfWork.GetCollection<User>()
            .Find(u => u.Id == request.ReportedUserId && u.Status != UserStatus.Banned)
            .AnyAsync() ? true : throw new NotFoundCustomException("Reported user not found");

        // Không cho phép tư report chính mình
        if (reporterId == request.ReportedUserId)
        {
            throw new BadRequestCustomException("You cannot report yourself");
        }

        // Kiểm tra xem đã report user này chưa (trong vòng 24h)
        DateTimeOffset last24Hours = HelperMethod.GetUtcPlus7TimeOffset().AddHours(-24);
        bool existingReport = await _unitOfWork.GetCollection<Report>()
            .Find(r => r.ReporterId == reporterId
                && r.ReportedUserId == request.ReportedUserId
                && r.CreatedAt >= last24Hours
                && r.Status == ReportStatus.Pending)
            .AnyAsync();

        if (existingReport)
        {
            throw new BadRequestCustomException("You have already reported this user in the last 24 hours");
        }

        // Đếm số lần user này bị report (trừ các report bị từ chối hoặc bác bỏ)
        long totalReportsCount = await _unitOfWork.GetCollection<Report>()
            .CountDocumentsAsync(r => r.ReportedUserId == request.ReportedUserId
                && r.Status != ReportStatus.Rejected
                && r.Status != ReportStatus.Dismissed);

        // Tự động tặng priority nếu user bị report nhiều lần
        ReportPriority priority = totalReportsCount switch
        {
            >= 30 => ReportPriority.Critical,
            >= 20 => ReportPriority.High,
            >= 10 => ReportPriority.Medium,
            _ => ReportPriority.Low
        };

        Report report = new()
        {
            ReportedUserId = request.ReportedUserId,
            ReporterId = reporterId,
            ReportType = request.ReportType,
            Description = request.Description,
            Status = ReportStatus.Pending,
            Priority = priority,
            RelatedContentId = request.RelatedContentId,
            RelatedContentType = request.RelatedContentType,
            Evidences = request.Evidences ?? [],
            TotalReportsCount = totalReportsCount + 1,
        };

        await _unitOfWork.GetCollection<Report>().InsertOneAsync(report);

        return;
    }

    public async Task AssignReportToModeratorAsync(string reportId, string moderatorId)
    {
        string currentUserId = GetCurrentUserId();
        string role = GetCurrentUserRole();

        // Chỉ admin hoặc moderator tự assign
        if (role != UserRole.Admin.ToString() && currentUserId != moderatorId)
        {
            throw new ForbiddenCustomException("Only admin can assign reports to other moderators");
        }

        // Kiểm tra moderator có tồn tại không
        bool isModeratorExisted = await _unitOfWork.GetCollection<User>()
            .Find(u => u.Id == moderatorId && u.Role == UserRole.Moderator)
            .AnyAsync() ? true : throw new NotFoundCustomException("Moderator not found");

        UpdateResult result = await _unitOfWork.GetCollection<Report>()
            .UpdateOneAsync(
                r => r.Id == reportId && r.IsDeleted == false,
                Builders<Report>.Update
                    .Set(r => r.AssignedModeratorId, moderatorId)
                    .Set(r => r.Status, ReportStatus.UnderReview)
                    .Set(r => r.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
            );

        if (result.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Failed to assign report to moderator");
        }
    }

    public async Task ProcessReportAsync(ProcessReportRequest request)
    {
        string moderatorId = GetCurrentUserId();
        string role = GetCurrentUserRole();

        // Chỉ moderator và admin mới xử lý được
        if (role != UserRole.Moderator.ToString() && role != UserRole.Admin.ToString())
        {
            throw new ForbiddenCustomException("You don't have permission to process reports");
        }

        Report report = await _unitOfWork.GetCollection<Report>()
            .Find(r => r.Id == request.ReportId && r.IsDeleted == false)
            .Project<Report>(Builders<Report>.Projection
                .Include(x => x.Id)
                .Include(x => x.ReportedUserId))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Report not found");

        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // Update report
            UpdateResult updateResult = await _unitOfWork.GetCollection<Report>()
                .UpdateOneAsync(session,
                    r => r.Id == request.ReportId,
                    Builders<Report>.Update
                        .Set(r => r.Status, request.Status)
                        .Set(r => r.ActionTaken, request.ActionTaken)
                        .Set(r => r.Note, request.Note)
                        .Set(r => r.ResolvedAt, HelperMethod.GetUtcPlus7TimeOffset())
                        .Set(r => r.AssignedModeratorId, moderatorId)
                        .Set(r => r.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
                );

            if (updateResult.ModifiedCount == 0)
            {
                throw new UnprocessableEntityCustomException("Failed to update report");
            }

            // Thực hiện hành động với user nếu báo cáo được approve
            if (request.Status == ReportStatus.Approved && request.ActionTaken != ReportAction.NoAction)
            {
                await ApplyActionToUserAsync(report.ReportedUserId, request);
            }

            return;
        });
    }

    private async Task ApplyActionToUserAsync(string userId, ProcessReportRequest request)
    {
        switch (request.ActionTaken)
        {
            case ReportAction.Warning:
                User userWarning = await _unitOfWork.GetCollection<User>()
                    .Find(u => u.Id == userId)
                    .Project<User>(Builders<User>.Projection
                        .Include(u => u.Id)
                        .Include(u => u.Email)
                        .Include(u => u.FullName))
                    .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("User not found");

                // Gửi email
                BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(EmailTemplateType.WarningReport, userWarning.Email, userWarning.FullName, userWarning.Email, request.Note!));
                break;

            case ReportAction.Suspended:
                if (!request.SuspensionDays.HasValue || request.SuspensionDays <= 0)
                {
                    throw new BadRequestCustomException("Suspension days must be greater than 0");
                }

                DateTimeOffset suspensionExpiry = HelperMethod.GetUtcPlus7TimeOffset().AddDays(request.SuspensionDays.Value);

                // Update user status
                User userSuspended = await _unitOfWork.GetCollection<User>()
                    .FindOneAndUpdateAsync(
                        u => u.Id == userId,
                        Builders<User>.Update
                            .Set(u => u.Status, UserStatus.Suspended)
                            .AddToSet(u => u.Restrictions, new Restriction
                            {
                                Type = RestrictionType.Suspended,
                                Action = null,
                                Reason = request.Note,
                                RestrictedAt = HelperMethod.GetUtcPlus7TimeOffset(),
                                Expired = request.SuspensionDays.HasValue
                                            ? suspensionExpiry
                                            : null
                            })
                            .Set(u => u.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()),
                        new FindOneAndUpdateOptions<User, User>
                        {
                            ReturnDocument = ReturnDocument.Before,
                            Projection = Builders<User>.Projection
                                .Include(u => u.Id)
                                .Include(u => u.Email)
                                .Include(u => u.FullName)
                        }
                    );

                // Xóa restriction sau khi hết hạn
                BackgroundJob.Schedule<IBackgoundService>(x => x.RemoveExpiredRestrictionAsync(userId), suspensionExpiry);

                // Gửi email
                BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(EmailTemplateType.TemporarySuspension, userSuspended.Email, userSuspended.FullName, userSuspended.Email, request.Note!, HelperMethod.GetUtcPlus7TimeOffset().AddDays(request.SuspensionDays.Value).ToString("dd/MM/yyyy HH:mm:ss")));

                break;

            case ReportAction.EntitlementRestriction:
                List<Restriction> accountRestrictions = [];

                foreach (RestrictionActionDetail restrictionActionDetail in request.RestrictionActionDetails)
                {
                    DateTimeOffset? restrictionExpiry = request.SuspensionDays.HasValue
                            ? HelperMethod.GetUtcPlus7TimeOffset().AddDays(request.SuspensionDays.Value)
                            : null;

                    accountRestrictions.Add(new Restriction
                    {
                        Type = RestrictionType.Suspended,
                        Action = restrictionActionDetail.RestrictionAction,
                        Reason = restrictionActionDetail.Note,
                        RestrictedAt = HelperMethod.GetUtcPlus7TimeOffset(),
                        Expired = restrictionExpiry
                    });

                    if (restrictionExpiry != null)
                    {
                        // Xóa restriction sau khi hết hạn
                        BackgroundJob.Schedule<IBackgoundService>(x => x.RemoveExpiredRestrictionAsync(userId), restrictionExpiry.Value);
                    }
                }

                User userRestrictedEntitlement = await _unitOfWork.GetCollection<User>()
                    .FindOneAndUpdateAsync(
                        u => u.Id == userId,
                        Builders<User>.Update
                            .AddToSetEach(u => u.Restrictions, accountRestrictions)
                            .Set(u => u.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()),
                        new FindOneAndUpdateOptions<User, User>
                        {
                            ReturnDocument = ReturnDocument.Before,
                            Projection = Builders<User>.Projection
                                .Include(u => u.Id)
                                .Include(u => u.Email)
                                .Include(u => u.FullName)
                        }
                    );

                // Gửi email
                // TODO: Thêm email template riêng cho restriction
                BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(EmailTemplateType.WarningReport, userRestrictedEntitlement.Email, userRestrictedEntitlement.FullName, userRestrictedEntitlement.Email, request.Note!));

                break;

            case ReportAction.ContentRemoval:
                // Logic xóa content n?u có RelatedContentId
                // TODO: Implement based on content type
                break;

            case ReportAction.PermanentBan:
                // Update user status
                User userPermanentBan = await _unitOfWork.GetCollection<User>()
                    .FindOneAndUpdateAsync(
                        u => u.Id == userId,
                        Builders<User>.Update
                            .Set(u => u.Status, UserStatus.Banned)
                            .AddToSet(u => u.Restrictions, new Restriction
                            {
                                Type = RestrictionType.Banned,
                                Action = null,
                                Reason = request.Note,
                                RestrictedAt = HelperMethod.GetUtcPlus7TimeOffset(),
                                Expired = null // Permanent
                            })
                            .Set(u => u.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()),
                        new FindOneAndUpdateOptions<User, User>
                        {
                            ReturnDocument = ReturnDocument.Before,
                            Projection = Builders<User>.Projection
                                .Include(u => u.Id)
                                .Include(u => u.Email)
                                .Include(u => u.FullName)
                        }
                    );

                // Gửi email
                BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(EmailTemplateType.PermanentBan, userPermanentBan.Email, userPermanentBan.FullName, userPermanentBan.Email, request.Note!));

                break;

            default:
                break;
        }
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

        IMongoCollection<Report> collection = _unitOfWork.GetCollection<Report>();

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

        UpdateResult result = await _unitOfWork.GetCollection<Report>()
            .UpdateOneAsync(
                r => r.Id == reportId && r.IsDeleted == false,
                Builders<Report>.Update
                    .Set(r => r.Priority, reportPriority)
                    .Set(r => r.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
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

        UpdateResult result = await _unitOfWork.GetCollection<Report>()
            .UpdateOneAsync(
                r => r.Id == reportId,
                Builders<Report>.Update
                    .Set(r => r.IsDeleted, true)
                    .Set(r => r.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
            );

        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Escalate báo cáo lên admin
    /// </summary>
    public async Task<bool> EscalateReportAsync(string reportId)
    {
        string currentUserId = GetCurrentUserId();

        UpdateResult result = await _unitOfWork.GetCollection<Report>()
            .UpdateOneAsync(
                r => r.Id == reportId && r.IsDeleted == false,
                Builders<Report>.Update
                    .Set(r => r.Status, ReportStatus.Escalated)
                    .Set(r => r.Priority, ReportPriority.Critical)
                    .Set(r => r.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
            );

        return result.ModifiedCount > 0;
    }

    public async Task RemoveExpiredRestrictionAsync(string userId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            UpdateResult updateResult = await _unitOfWork.GetCollection<User>()
            .UpdateOneAsync(session,
                u => u.Id == userId,
                Builders<User>.Update
                    .PullFilter(u => u.Restrictions, r => r.Type != RestrictionType.None &&
                        r.Expired != null &&
                        r.Expired <= HelperMethod.GetUtcPlus7TimeOffset())
                    .Set(u => u.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
            );

            if (updateResult.MatchedCount == 0)
            {
                throw new NotFoundCustomException("User not found.");
            }

            if (updateResult.ModifiedCount == 0)
            {
                throw new UnprocessableEntityCustomException("No restrictions were removed.");
            }

            UpdateResult updateUserStatus = await _unitOfWork.GetCollection<User>()
            .UpdateOneAsync(session,
                u => u.Id == userId && !u.Restrictions.Any(),
                Builders<User>.Update
                    .Set(u => u.Status, UserStatus.Active)
                    .Set(u => u.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
            );
        });
    }
}
