using EkofyApp.Application.Models.UserSubscriptions;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.UserSubscriptions;
public sealed class UserSubscriptionService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IUserSubscriptionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public IQueryable<UserSubscription> GetUserSubscriptions()
    {
        return _unitOfWork.GetCollection<UserSubscription>().AsQueryable();
    }

    public async Task CreateUserSubscriptionAsync(CreateUserSubscriptionRequest createUserSubscriptionRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        await _unitOfWork.GetCollection<UserSubscription>().InsertOneAsync(new UserSubscription()
        {
            UserId = userId,
            SubscriptionId = createUserSubscriptionRequest.SubscriptionId,
            PeriodStart = createUserSubscriptionRequest.PeriodStart,
            PeriodEnd = createUserSubscriptionRequest.PeriodEnd,
            AutoRenew = createUserSubscriptionRequest.AutoRenew,
        });
    }

    public async Task UpdateStatusUserSubscriptionAsync(UpdateUserSubscriptionRequest updateUserSubscriptionRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // TODO: Làm sao để biết là document nào mới đúng là đang cần tìm
        // Vì có thể có nhiều UserSubscription với cùng UserId và SubscriptionId
        // Nên cần có thêm một trường nào đó để phân biệt
        FilterDefinitionBuilder<UserSubscription> filterDefinitionBuilder = Builders<UserSubscription>.Filter;

        FilterDefinition<UserSubscription> filter = filterDefinitionBuilder.And(
            filterDefinitionBuilder.Eq(us => us.UserId, userId),
            filterDefinitionBuilder.Eq(us => us.SubscriptionId, updateUserSubscriptionRequest.SubscriptionId),
            filterDefinitionBuilder.Eq(us => us.Status, SubscriptionStatus.Active));

        UpdateDefinition<UserSubscription> update = Builders<UserSubscription>.Update
            .Set(us => us.CancelAtEndOfPeriod, updateUserSubscriptionRequest.CancelAtEndOfPeriod)
            .Set(us => us.CanceledAt, updateUserSubscriptionRequest.CanceledAt)
            .Set(us => us.Status, SubscriptionStatus.Deprecated);

        await _unitOfWork.GetCollection<UserSubscription>().UpdateOneAsync(filter, update);
    }
}
