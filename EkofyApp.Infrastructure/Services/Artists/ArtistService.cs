using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Application.ServiceInterfaces.Jobs;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Hangfire;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Artists;

public sealed class ArtistService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IRedisCacheService redisCacheService, IUserSubscriptionService userSubscriptionService, IEffectiveEntitlementService effectiveEntitlementService) : IArtistService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;
    private readonly IUserSubscriptionService _userSubscriptionService = userSubscriptionService;
    private readonly IEffectiveEntitlementService _effectiveEntitlementService = effectiveEntitlementService;

    public IQueryable<Artist> GetArtistsQueryable()
    {
        // Trả về IQueryable của Artist từ UnitOfWork
        return _unitOfWork.GetCollection<Artist>().AsQueryable();
    }

    public async Task<bool> CreateArtistAsync(CreateArtistRequest createArtistRequest)
    {
        Artist artist = new()
        {
            UserId = createArtistRequest.UserId,
            StageName = createArtistRequest.Name,
            Biography = createArtistRequest.Biography,
            IdentityCard = createArtistRequest.IdentityCard,
        };

        await _unitOfWork.GetCollection<Artist>().InsertOneAsync(artist);

        return true;
    }

    public async Task UpdateArtistAsync(UpdateArtistRequest updateArtistRequest)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            string artistId = _httpContextAccessor.HttpContext?.User.FindFirst("artistId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            List<UpdateDefinition<Artist>> updateDefinitions =
            [
                Builders<Artist>.Update.Set(a => a.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
            ];

            if (!string.IsNullOrWhiteSpace(updateArtistRequest.StageName))
            {
                updateDefinitions.Add(Builders<Artist>.Update.Set(a => a.StageName, updateArtistRequest.StageName));
            }

            if (!string.IsNullOrWhiteSpace(updateArtistRequest.Biography))
            {
                updateDefinitions.Add(Builders<Artist>.Update.Set(a => a.Biography, updateArtistRequest.Biography));
            }

            if (updateArtistRequest.AvatarImage != null)
            {
                updateDefinitions.Add(Builders<Artist>.Update.Set(a => a.AvatarImage, updateArtistRequest.AvatarImage));
            }

            if (updateArtistRequest.BannerImage != null)
            {
                updateDefinitions.Add(Builders<Artist>.Update.Set(a => a.BannerImage, updateArtistRequest.BannerImage));
            }

            UpdateDefinition<Artist> update = Builders<Artist>.Update.Combine(updateDefinitions);
            UpdateResult result = await _unitOfWork.GetCollection<Artist>().UpdateOneAsync(
                session,
                a => a.Id == artistId,
                update
            );

            if (result.MatchedCount == 0)
            {
                throw new NotFoundCustomException($"Not found artist with id {artistId}");
            }
            if (result.ModifiedCount < updateDefinitions.Count)
            {
                throw new BadRequestCustomException("No changes were made to the artist profile");
            }
        });
    }

    public async Task<IEnumerable<PendingArtistRegistrationResponse>> GetPendingRegistrationsAsync(int pageNumber = 1, int pageSize = 20)
    {
        var result = await _redisCacheService.GetPendingArtistRegistrationsAsync(pageNumber, pageSize);

        if (!result.Success || result.Value == null)
        {
            return [];
        }

        return result.Value.Select(pending => new PendingArtistRegistrationResponse
        {
            Id = pending.UserId,
            Email = pending.Email,
            FullName = pending.FullName,
            StageName = pending.StageName,
            ArtistType = pending.ArtistType,
            Gender = pending.Gender,
            BirthDate = pending.BirthDate,
            PhoneNumber = pending.PhoneNumber,
            RequestedAt = pending.RequestedAt,
            TimeToLive = result.TimeToLive,
            IdentityCardNumber = pending.IdentityCard.Number,
            IdentityCardFullName = pending.IdentityCard.FullName,
            IdentityCardDateOfBirth = pending.IdentityCard.DateOfBirth,
            PlaceOfOrigin = pending.IdentityCard.PlaceOfOrigin,
            PlaceOfResidence = pending.IdentityCard.PlaceOfResidence.AddressLine ?? string.Empty,
            FrontImageUrl = pending.IdentityCard.FrontImage,
            BackImageUrl = pending.IdentityCard.BackImage
        });
    }

    public async Task ApproveArtistRegistrationAsync(ArtistRegistrationApprovalRequest approvalRequest)
    {
        string redisKey = $"artist:{approvalRequest.UserId}:pendingRegistration";

        if (!_redisCacheService.TryGetGeneric<PendingArtistRegistration>(redisKey, out PendingArtistRegistration? pendingRegistration))
        {
            throw new NotFoundCustomException("Artist registration request not found or has expired");
        }

        if (pendingRegistration == null)
        {
            throw new NotFoundCustomException("Artist registration request not found");
        }

        // Create user and artist in database within transaction
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // Create user
            User user = new()
            {
                Id = pendingRegistration.UserId,
                Email = pendingRegistration.Email,
                PasswordHash = pendingRegistration.PasswordHash,
                FullName = pendingRegistration.FullName,
                BirthDate = pendingRegistration.BirthDate,
                Gender = pendingRegistration.Gender,
                PhoneNumber = pendingRegistration.PhoneNumber,
                Role = UserRole.Artist,
                Status = UserStatus.Active, // Approved users are active
                IsLinkedWithGoogle = false,
            };

            // Create artist
            Artist artist = new()
            {
                UserId = pendingRegistration.UserId,
                StageName = pendingRegistration.StageName,
                Email = pendingRegistration.Email,
                ArtistType = pendingRegistration.ArtistType,
                Members = pendingRegistration.Members,
                IdentityCard = pendingRegistration.IdentityCard,
            };

            await _unitOfWork.GetCollection<User>().InsertOneAsync(session, user);
            await _unitOfWork.GetCollection<Artist>().InsertOneAsync(session, artist);

            // Tạo mới UserSubscription với gói Free
            await _userSubscriptionService.CreateUserSubscriptionAsync(session, user.Id, string.Empty, HelperMethod.GetUtcPlus7TimeOffset());

            // Xây dựng quyền lợi mặc định cho Artist (gói Free)
            await _effectiveEntitlementService.BuildFreeTierAsync(session, user.Id, UserRole.Artist);

            // Remove from Redis after successful creation
            await _redisCacheService.RemoveAsync(redisKey);

            // Send approval email to user
            BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(EmailTemplateType.RegisterApprove, user.Email, user.FullName, user.Email));
        });
    }

    public async Task RejectArtistRegistrationAsync(ArtistRegistrationApprovalRequest approvalRequest)
    {
        string redisKey = $"artist:{approvalRequest.UserId}:pendingRegistration";

        if (!await _redisCacheService.ExistsAsync(redisKey))
        {
            throw new NotFoundCustomException("Artist registration request not found or has expired");
        }

        // Simply remove from Redis - rejection means no database record is created
        await _redisCacheService.RemoveAsync(redisKey);

        // Send rejection email to user
        BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(EmailTemplateType.RegisterApprove, approvalRequest.Email, approvalRequest.FullName, approvalRequest.Email));
    }
}
