using EkofyApp.Application.Models.ApprovalHistories;
using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.ApprovalHistories;
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
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Stripe;

namespace EkofyApp.Infrastructure.Services.Artists;

public sealed class ArtistService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IRedisCacheService redisCacheService, IUserSubscriptionService userSubscriptionService, IEffectiveEntitlementService effectiveEntitlementService, IApprovalHistoryService approvalHistoryService) : IArtistService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;
    private readonly IUserSubscriptionService _userSubscriptionService = userSubscriptionService;
    private readonly IEffectiveEntitlementService _effectiveEntitlementService = effectiveEntitlementService;
    private readonly IApprovalHistoryService _approvalHistoryService = approvalHistoryService;

    public IQueryable<Artist> GetArtistsQueryable()
    {
        // Trả về IQueryable của Artist từ UnitOfWork
        return _unitOfWork.GetCollection<Artist>().AsQueryable();
    }

    public IQueryable<Artist> SearchArtists(string stageName)
    {
        IQueryable<Artist> query = _unitOfWork.GetCollection<Artist>().AsQueryable();

        if (string.IsNullOrEmpty(stageName))
        {
            return query;
        }

        string unsignedSearchTerm = HelperMethod.ToUnsigned(stageName);
        query = query.Where(t => t.StageNameUnsigned.Contains(unsignedSearchTerm));

        return query;
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

    public async Task UpdateProfileAsync(UpdateArtistRequest updateArtistRequest)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // Check for email conflict if email is being updated
            if (!string.IsNullOrWhiteSpace(updateArtistRequest.Email))
            {
                if (await _unitOfWork.GetCollection<Artist>().Find(a => a.Email == updateArtistRequest.Email).AnyAsync() == true)
                {
                    throw new ConflictCustomException($"Email {updateArtistRequest.Email} is already in use");
                }
            }

            // Get user and artist information
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
            string artistId = _httpContextAccessor.HttpContext?.User.FindFirst("artistId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            User user = _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == userId)
                .Project<User>(Builders<User>.Projection
                    .Include(x => x.FullName)
                    .Include(x => x.StripeCustomerId))
                .FirstOrDefault() ?? throw new NotFoundCustomException($"Not found user with id {userId}");

            bool isArtistExisted = await _unitOfWork.GetCollection<Artist>()
                .Find(a => a.Id == artistId)
                .Project<Artist>(Builders<Artist>.Projection
                    .Include(x => x.Email)
                    .Include(x => x.StageName))
                .AnyAsync() ? true : throw new NotFoundCustomException($"Not found artist with id {artistId}");

            // Create list of update definitions for Artist
            List<UpdateDefinition<Artist>> updates =
            [
                Builders<Artist>.Update.Set(a => a.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
            ];

            // Create list of update definitions for User
            List<UpdateDefinition<User>> updatesUser =
            [
                Builders<User>.Update.Set(u => u.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
            ];

            // Artist-specific updates
            if (!string.IsNullOrWhiteSpace(updateArtistRequest.StageName))
            {
                updates.Add(Builders<Artist>.Update.Set(a => a.StageName, updateArtistRequest.StageName));
                updates.Add(Builders<Artist>.Update.Set(a => a.StageNameUnsigned, HelperMethod.ToUnsigned(updateArtistRequest.StageName)));
            }

            if (!string.IsNullOrWhiteSpace(updateArtistRequest.Biography))
            {
                updates.Add(Builders<Artist>.Update.Set(a => a.Biography, updateArtistRequest.Biography));
            }

            if (updateArtistRequest.AvatarImage != null)
            {
                updates.Add(Builders<Artist>.Update.Set(a => a.AvatarImage, updateArtistRequest.AvatarImage));
            }

            if (updateArtistRequest.BannerImage != null)
            {
                updates.Add(Builders<Artist>.Update.Set(a => a.BannerImage, updateArtistRequest.BannerImage));
            }

            if (updateArtistRequest.Gender != null)
            {
                updatesUser.Add(Builders<User>.Update.Set(a => a.Gender, updateArtistRequest.Gender));
            }

            if (updateArtistRequest.BirthDate != null)
            {
                updatesUser.Add(Builders<User>.Update.Set(a => a.BirthDate, updateArtistRequest.BirthDate));
            }

            // Track which fields are being updated for Stripe
            bool isEmailUpdated = false;
            bool isFullNameUpdated = false;

            // Email updates (both Artist and User)
            if (!string.IsNullOrWhiteSpace(updateArtistRequest.Email))
            {
                updates.Add(Builders<Artist>.Update.Set(a => a.Email, updateArtistRequest.Email));
                updatesUser.Add(Builders<User>.Update.Set(u => u.Email, updateArtistRequest.Email));
                isEmailUpdated = true;
            }

            // User-specific updates
            if (!string.IsNullOrWhiteSpace(updateArtistRequest.PhoneNumber))
            {
                updatesUser.Add(Builders<User>.Update.Set(u => u.PhoneNumber, updateArtistRequest.PhoneNumber));
            }

            if (!string.IsNullOrWhiteSpace(updateArtistRequest.FullName))
            {
                updatesUser.Add(Builders<User>.Update.Set(u => u.FullName, updateArtistRequest.FullName));
                isFullNameUpdated = true;
            }

            // Update Stripe customer if needed
            if (!string.IsNullOrWhiteSpace(user.StripeCustomerId))
            {
                if (isEmailUpdated && isFullNameUpdated)
                {
                    CustomerUpdateOptions customerUpdateOptions = new()
                    {
                        Email = updateArtistRequest.Email,
                        Name = updateArtistRequest.FullName,
                    };

                    CustomerService customerService = new();
                    await customerService.UpdateAsync(user.StripeCustomerId, customerUpdateOptions);
                }
                else if (isEmailUpdated)
                {
                    CustomerUpdateOptions customerUpdateOptions = new()
                    {
                        Email = updateArtistRequest.Email,
                    };

                    CustomerService customerService = new();
                    await customerService.UpdateAsync(user.StripeCustomerId, customerUpdateOptions);
                }
                else if (isFullNameUpdated)
                {
                    CustomerUpdateOptions customerUpdateOptions = new()
                    {
                        Name = updateArtistRequest.FullName,
                    };

                    CustomerService customerService = new();
                    await customerService.UpdateAsync(user.StripeCustomerId, customerUpdateOptions);
                }
            }

            // Combine all updates
            UpdateDefinition<Artist> updateDefinition = Builders<Artist>.Update.Combine(updates);
            UpdateDefinition<User> updateDefinitionUser = Builders<User>.Update.Combine(updatesUser);

            // Update the artist and user
            UpdateResult updateArtist = await _unitOfWork.GetCollection<Artist>().UpdateOneAsync(session, x => x.Id == artistId, updateDefinition);
            UpdateResult updateUser = await _unitOfWork.GetCollection<User>().UpdateOneAsync(session, x => x.Id == userId, updateDefinitionUser);

            if (updateArtist.ModifiedCount == 0 && updateUser.ModifiedCount == 0)
            {
                throw new UnprocessableEntityCustomException("No changes were made to the artist profile");
            }
        });
    }

    public async Task<PaginatedData<PendingArtistRegistrationResponse>> GetPendingRegistrationsAsync(int pageNumber = 1, int pageSize = 20)
    {
        ICacheResult<PaginatedData<PendingArtistRegistrationRequest>> result = await _redisCacheService.GetPendingArtistRegistrationsAsync(pageNumber, pageSize);

        PaginatedData<PendingArtistRegistrationResponse> paginatedData;

        if (!result.Success || result.Value == null)
        {
            return new()
            {
                Items = Enumerable.Empty<PendingArtistRegistrationResponse>(),
                TotalCount = 0
            };
        }

        paginatedData = new()
        {
            Items = result.Value.Items.Select(pending => new PendingArtistRegistrationResponse
            {
                Id = pending.UserId,
                Email = pending.Email,
                FullName = pending.FullName,
                StageName = pending.StageName,
                StageNameUnsigned = pending.StageNameUnsigned,
                ArtistType = pending.ArtistType,
                Gender = pending.Gender,
                BirthDate = pending.BirthDate,
                PhoneNumber = pending.PhoneNumber,
                AvatarImage = pending.AvatarImage,
                Members = pending.Members,
                RequestedAt = pending.RequestedAt,
                TimeToLive = result.TimeToLive,
                IdentityCardNumber = pending.IdentityCard.Number,
                IdentityCardFullName = pending.IdentityCard.FullName,
                IdentityCardDateOfBirth = pending.IdentityCard.DateOfBirth,
                PlaceOfOrigin = pending.IdentityCard.PlaceOfOrigin,
                PlaceOfResidence = pending.IdentityCard.PlaceOfResidence.AddressLine ?? string.Empty,
                FrontImageUrl = pending.IdentityCard.FrontImage,
                BackImageUrl = pending.IdentityCard.BackImage,
            }),
            TotalCount = result.Value.TotalCount
        };
        
        return paginatedData;
    }

    public async Task<PendingArtistRegistrationResponse> GetPendingRegistrationByIdAsync(string artistRegistrationId)
    {
        string redisKey = $"artist:{artistRegistrationId}:pendingRegistration";
        
        ICacheResult<PendingArtistRegistrationRequest> cacheResult = await _redisCacheService.TryGetGenericAsync<PendingArtistRegistrationRequest>(redisKey);
        
        if (!cacheResult.Success || cacheResult.Value == null)
        {
            throw new NotFoundCustomException($"Artist registration with ID {artistRegistrationId} not found or expired.");
        }

        PendingArtistRegistrationRequest pending = cacheResult.Value;
        
        return new PendingArtistRegistrationResponse
        {
            Id = pending.UserId,
            Email = pending.Email,
            FullName = pending.FullName,
            StageName = pending.StageName,
            StageNameUnsigned = pending.StageNameUnsigned,
            ArtistType = pending.ArtistType,
            Gender = pending.Gender,
            BirthDate = pending.BirthDate,
            PhoneNumber = pending.PhoneNumber,
            AvatarImage = pending.AvatarImage,
            Members = pending.Members,
            RequestedAt = pending.RequestedAt,
            TimeToLive = cacheResult.TimeToLive,
            IdentityCardNumber = pending.IdentityCard.Number,
            IdentityCardFullName = pending.IdentityCard.FullName,
            IdentityCardDateOfBirth = pending.IdentityCard.DateOfBirth,
            PlaceOfOrigin = pending.IdentityCard.PlaceOfOrigin,
            PlaceOfResidence = pending.IdentityCard.PlaceOfResidence.AddressLine ?? string.Empty,
            FrontImageUrl = pending.IdentityCard.FrontImage,
            BackImageUrl = pending.IdentityCard.BackImage,
        };
    }

    public async Task ApproveArtistRegistrationAsync(ArtistRegistrationApprovalRequest approvalRequest)
    {
        string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string redisKey = $"artist:{approvalRequest.UserId}:pendingRegistration";

        if (!_redisCacheService.TryGetGeneric<PendingArtistRegistrationRequest>(redisKey, out PendingArtistRegistrationRequest? pendingRegistration))
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
                StageNameUnsigned = pendingRegistration.StageNameUnsigned,
                Email = pendingRegistration.Email,
                AvatarImage = pendingRegistration.AvatarImage,
                ArtistType = pendingRegistration.ArtistType,
                Members = pendingRegistration.Members,
                LegalDocuments = pendingRegistration.LegalDocuments,
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

            // Ẩn passwordHash trước khi lưu snapshot
            pendingRegistration = pendingRegistration with { PasswordHash = string.Empty };

            // Lưu snapshot
            ApprovalHistoryRequest approvalHistoryRequest = new()
            {
                TargetId = user.Id,
                ApprovalType = ApprovalType.ArtistRegistration,
                ActionByUserId = currentUserId,
                ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
                Action = HistoryActionType.Approved,
                Notes = approvalRequest.RejectionReason,
                Snapshot = pendingRegistration,
            };
            await _approvalHistoryService.CreateApprovalHistoryAsync(approvalHistoryRequest);

            // Send approval email to user
            BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(EmailTemplateType.RegisterApprove, user.Email, user.FullName, user.Email));
        });
    }

    public async Task RejectArtistRegistrationAsync(ArtistRegistrationApprovalRequest approvalRequest)
    {
        string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string redisKey = $"artist:{approvalRequest.UserId}:pendingRegistration";

        if (!_redisCacheService.TryGetGeneric<PendingArtistRegistrationRequest>(redisKey, out PendingArtistRegistrationRequest? pendingRegistration))
        {
            throw new NotFoundCustomException("Artist registration request not found or has expired");
        }

        if (pendingRegistration == null)
        {
            throw new NotFoundCustomException("Artist registration request not found");
        }

        // Simply remove from Redis - rejection means no database record is created
        await _redisCacheService.RemoveAsync(redisKey);

        // Ẩn passwordHash trước khi lưu snapshot
        pendingRegistration = pendingRegistration with { PasswordHash = string.Empty };

        // Lưu snapshot
        ApprovalHistoryRequest approvalHistoryRequest = new()
        {
            TargetId = pendingRegistration.UserId,
            ApprovalType = ApprovalType.ArtistRegistration,
            ActionByUserId = currentUserId,
            ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
            Action = HistoryActionType.Rejected,
            Notes = approvalRequest.RejectionReason, // Dùng trường Notes để lưu lý do từ chối nếu có
            Snapshot = pendingRegistration,
        };

        await _approvalHistoryService.CreateApprovalHistoryAsync(approvalHistoryRequest);

        // Send rejection email to user
        BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(EmailTemplateType.RegisterReject, approvalRequest.Email, approvalRequest.FullName, approvalRequest.Email, approvalRequest.RejectionReason ?? string.Empty));
    }
}
