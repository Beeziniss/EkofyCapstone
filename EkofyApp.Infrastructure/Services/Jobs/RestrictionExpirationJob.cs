using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
using Hangfire;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Jobs;

/// <summary>
/// Background job ?? t? ??ng g? restriction khi h?t h?n
/// </summary>
public sealed class RestrictionExpirationJob(
    IUnitOfWork unitOfWork,
    ILogger<RestrictionExpirationJob> logger)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<RestrictionExpirationJob> _logger = logger;

    /// <summary>
    /// Ch?y job ki?m tra và g? restriction ?ã h?t h?n
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task CheckAndRemoveExpiredRestrictionsAsync()
    {
        try
        {
            _logger.LogInformation("Starting restriction expiration check job...");

            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Tìm t?t c? restriction ?ã h?t h?n nh?ng v?n còn active
            List<UserRestriction> expiredRestrictions = await _unitOfWork.GetCollection<UserRestriction>()
                .Find(r => r.IsActive == true 
                    && r.EndDate != null 
                    && r.EndDate <= now
                    && r.RestrictionType == RestrictionType.Suspended)
                .ToListAsync();

            if (expiredRestrictions.Count == 0)
            {
                _logger.LogInformation("No expired restrictions found");
                return;
            }

            _logger.LogInformation("Found {Count} expired restrictions to process", expiredRestrictions.Count);

            foreach (UserRestriction restriction in expiredRestrictions)
            {
                try
                {
                    // Deactivate restriction
                    await _unitOfWork.GetCollection<UserRestriction>()
                        .UpdateOneAsync(
                            r => r.Id == restriction.Id,
                            Builders<UserRestriction>.Update
                                .Set(r => r.IsActive, false)
                                .Set(r => r.UpdatedAt, now)
                        );

                    // Check if user has any other active restrictions
                    bool hasOtherActiveRestrictions = await _unitOfWork.GetCollection<UserRestriction>()
                        .Find(r => r.UserId == restriction.UserId 
                            && r.IsActive == true 
                            && r.Id != restriction.Id)
                        .AnyAsync();

                    // If no other active restrictions, reactivate user
                    if (!hasOtherActiveRestrictions)
                    {
                        await _unitOfWork.GetCollection<User>()
                            .UpdateOneAsync(
                                u => u.Id == restriction.UserId,
                                Builders<User>.Update
                                    .Set(u => u.Status, UserStatus.Active)
                                    .Set(u => u.UpdatedAt, now)
                            );

                        _logger.LogInformation("User {UserId} has been reactivated after suspension expired", restriction.UserId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing restriction {RestrictionId} for user {UserId}", 
                        restriction.Id, restriction.UserId);
                }
            }

            _logger.LogInformation("Restriction expiration check job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in restriction expiration check job");
            throw;
        }
    }
}
