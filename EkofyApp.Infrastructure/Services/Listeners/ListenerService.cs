using EkofyApp.Application.Models.Listeners;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Listeners;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Stripe;

namespace EkofyApp.Infrastructure.Services.Listeners;
public sealed class ListenerService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IListenerService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public IQueryable<Listener> GetListeners()
    {
        return _unitOfWork.GetCollection<Listener>().AsQueryable();
    }

    public IQueryable<Listener> SearchListeners(string displayName)
    {
        IQueryable<Listener> query = _unitOfWork.GetCollection<Listener>().AsQueryable();

        if (string.IsNullOrEmpty(displayName))
        {
            return query;
        }

        string unsignedSearchTerm = HelperMethod.ToUnsigned(displayName);
        query = query.Where(t => t.DisplayNameUnsigned.Contains(unsignedSearchTerm));

        return query;
    }

    // TODO: Thêm xác nhận OTP khi thay đổi email
    public async Task UpdateProfileAsync(UpdateListenerRequest updateListenerRequest)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            if (await _unitOfWork.GetCollection<Listener>().Find(l => l.Email == updateListenerRequest.Email).AnyAsync() == true)
            {
                throw new ConflictCustomException($"Email {updateListenerRequest.Email} is already in use");
            }

            // Find the listener by ID
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
            string listenerId = _httpContextAccessor.HttpContext?.User.FindFirst("listenerId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            User user = _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == userId)
                .Project<User>(Builders<User>.Projection
                    .Include(x => x.FullName)
                    .Include(x => x.StripeCustomerId))
                .FirstOrDefault() ?? throw new NotFoundCustomException($"Not found fullname with user {userId}");

            Listener listener = await _unitOfWork.GetCollection<Listener>()
                .Find(l => l.Id == listenerId)
                .Project<Listener>(Builders<Listener>.Projection
                    .Include(x => x.Email)
                    .Include(x => x.DisplayName))
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found listener with id {listenerId}");

            // Create list of update definitions
            List<UpdateDefinition<Listener>> updates =
            [
                Builders<Listener>.Update.Set(l => l.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
            ];
            List<UpdateDefinition<User>> updatesUser = [
                Builders<User>.Update.Set(u => u.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
            ];

            if (!string.IsNullOrWhiteSpace(updateListenerRequest.DisplayName))
            {
                updates.Add(Builders<Listener>.Update.Set(l => l.DisplayName, updateListenerRequest.DisplayName));
                updates.Add(Builders<Listener>.Update.Set(l => l.DisplayNameUnsigned, HelperMethod.ToUnsigned(updateListenerRequest.DisplayName)));
            }

            if (updateListenerRequest.AvatarImage != null)
            {
                updates.Add(Builders<Listener>.Update.Set(l => l.AvatarImage, updateListenerRequest.AvatarImage));
            }

            if (updateListenerRequest.BannerImage != null)
            {
                updates.Add(Builders<Listener>.Update.Set(l => l.BannerImage, updateListenerRequest.BannerImage));
            }

            bool isEmailUpdated = false;
            bool isFullNameUpdated = false;

            if (!string.IsNullOrWhiteSpace(updateListenerRequest.Email))
            {
                updates.Add(Builders<Listener>.Update.Set(l => l.Email, updateListenerRequest.Email));
                updatesUser.Add(Builders<User>.Update.Set(u => u.Email, updateListenerRequest.Email));
                isEmailUpdated = true;
            }

            if (!string.IsNullOrWhiteSpace(updateListenerRequest.PhoneNumber))
            {
                updatesUser.Add(Builders<User>.Update.Set(u => u.PhoneNumber, updateListenerRequest.PhoneNumber));
            }

            if (!string.IsNullOrWhiteSpace(updateListenerRequest.FullName))
            {
                updatesUser.Add(Builders<User>.Update.Set(l => l.FullName, updateListenerRequest.FullName));
                isFullNameUpdated = true;
            }

            if (!string.IsNullOrWhiteSpace(user.StripeCustomerId))
            {
                if (isEmailUpdated && isFullNameUpdated)
                {
                    CustomerUpdateOptions customerUpdateOptions = new()
                    {
                        Email = listener.Email,
                        Name = user.FullName,
                    };

                    CustomerService customerService = new();
                    await customerService.UpdateAsync(user.StripeCustomerId, customerUpdateOptions);
                }
                else if (isEmailUpdated)
                {
                    CustomerUpdateOptions customerUpdateOptions = new()
                    {
                        Email = listener.Email,
                    };

                    CustomerService customerService = new();
                    await customerService.UpdateAsync(user.StripeCustomerId, customerUpdateOptions);
                }
                else if (isFullNameUpdated)
                {
                    CustomerUpdateOptions customerUpdateOptions = new()
                    {
                        Name = user.FullName,
                    };

                    CustomerService customerService = new();
                    await customerService.UpdateAsync(user.StripeCustomerId, customerUpdateOptions);
                }
                else
                {
                }
            }

            // Combine all updates
            UpdateDefinition<Listener> updateDefinition = Builders<Listener>.Update.Combine(updates);
            UpdateDefinition<User> updateDefinitionUser = Builders<User>.Update.Combine(updatesUser);

            // Update the listener
            UpdateResult update = await _unitOfWork.GetCollection<Listener>().UpdateOneAsync(session, x => x.Id == listenerId, updateDefinition);
            UpdateResult updateUser = await _unitOfWork.GetCollection<User>().UpdateOneAsync(session, x => x.Id == userId, updateDefinitionUser);

            if (update.ModifiedCount < updates.Count && updateUser.ModifiedCount < updatesUser.Count)
            {
                throw new UnprocessableEntityCustomException("No changes were made to the listener profile");
            }
        });
    }
}
