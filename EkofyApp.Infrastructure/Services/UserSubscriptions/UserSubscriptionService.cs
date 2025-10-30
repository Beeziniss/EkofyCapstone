using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Security.Claims;

namespace EkofyApp.Infrastructure.Services.UserSubscriptions;
public sealed class UserSubscriptionService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IUserSubscriptionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public IQueryable<UserSubscription> GetUserSubscriptions()
    {
        return _unitOfWork.GetCollection<UserSubscription>().AsQueryable();
    }

    public async Task CreateUserSubscriptionAsync(IClientSessionHandle? session, string userId, string subscriptionId, DateTimeOffset periodStart, DateTimeOffset? periodEnd = null)
    {
        // Mặc định nếu không có subscriptionId thì sẽ lấy gói Free
        if (string.IsNullOrEmpty(subscriptionId))
        {
            // Hiện tại gói Free là duy nhất, không cần xet version
            subscriptionId = await _unitOfWork.GetCollection<Subscription>()
                .Find(x => x.Tier == SubscriptionTier.Free && x.Status == SubscriptionStatus.Active)
                .Project(x => x.Id)
                .FirstOrDefaultAsync(); 
        }

        await _unitOfWork.GetCollection<UserSubscription>().InsertOneAsync(session, new UserSubscription()
        {
            UserId = userId,
            SubscriptionId = subscriptionId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
        });
    }

    public async Task CreateUserSubscriptionAsync(IClientSessionHandle? session, string userId, string subscriptionId, string stripeSubscriptionId, DateTimeOffset periodStart, DateTimeOffset? periodEnd = null)
    {
        // Mặc định nếu không có subscriptionId thì sẽ lấy gói Free
        if (string.IsNullOrEmpty(subscriptionId))
        {
            // Hiện tại gói Free là duy nhất, không cần xet version
            subscriptionId = await _unitOfWork.GetCollection<Subscription>()
                .Find(x => x.Tier == SubscriptionTier.Free && x.Status == SubscriptionStatus.Active)
                .Project(x => x.Id)
                .FirstOrDefaultAsync();
        }

        await _unitOfWork.GetCollection<UserSubscription>().InsertOneAsync(session, new UserSubscription()
        {
            UserId = userId,
            SubscriptionId = subscriptionId,
            StripeSubscriptionId = stripeSubscriptionId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
        });
    }

    public async Task UpdateStatusUserSubscriptionAsync(IClientSessionHandle? session, string userId, bool cancelAtEndOfPeriod, DateTimeOffset? canceledAt, bool status)
    {
        // TODO: Làm sao để biết là document nào mới đúng là đang cần tìm
        // Vì có thể có nhiều UserSubscription với cùng UserId và SubscriptionId
        // Nên cần có thêm một trường nào đó để phân biệt
        // Resolved: Lấy cái đang có trạng thái là Active
        await _unitOfWork.GetCollection<UserSubscription>().UpdateOneAsync(session, x => x.IsActive == status && x.UserId == userId, Builders<UserSubscription>.Update
            .Set(x => x.CancelAtEndOfPeriod, cancelAtEndOfPeriod)
            .Set(x => x.CanceledAt, canceledAt)
            .Set(x => x.IsActive, status)
            .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));
    }

    public async Task VerifyUserSubscriptionAsync()
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
        string role = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string currentSubscriptionId = await _unitOfWork.GetCollection<UserSubscription>()
            .Find(x => x.UserId == userId && x.IsActive == true)
            .Project(x => x.SubscriptionId)
            .FirstOrDefaultAsync();

        SubscriptionTier currentSubscriptionTier = await _unitOfWork.GetCollection<Subscription>()
            .Find(x => x.Id == currentSubscriptionId)
            .Project(x => x.Tier)
            .FirstOrDefaultAsync();

        SubscriptionTier tier = Enum.Parse<UserRole>(role, true) == UserRole.Listener ? SubscriptionTier.Premium : SubscriptionTier.Pro;

        if (tier == currentSubscriptionTier)
        {
            throw new ConflictCustomException("You already have subscription premium/pro.");
        }
    }
}
