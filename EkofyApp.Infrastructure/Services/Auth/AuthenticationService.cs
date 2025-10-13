using AutoMapper;
using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.Models.Auth;
using EkofyApp.Application.Models.Auth.Admins;
using EkofyApp.Application.Models.Auth.Artists;
using EkofyApp.Application.Models.Auth.Listeners;
using EkofyApp.Application.Models.Auth.Moderators;
using EkofyApp.Application.Models.Listeners;
using EkofyApp.Application.Models.Projections;
using EkofyApp.Application.Models.Users;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Authentication;
using EkofyApp.Application.ServiceInterfaces.Jobs;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Hangfire;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;
using System.Text;
using LoginRequest = EkofyApp.Application.Models.Auth.LoginRequest;

namespace EkofyApp.Infrastructure.Services.Auth;

public sealed class AuthenticationService(
    IUnitOfWork unitOfWork,
    IUserSubscriptionService userSubscriptionService,
    IEffectiveEntitlementService effectiveEntitlementService,
    IJsonWebToken jsonWebToken,
    IMapper mapper,
    IHttpContextAccessor httpContextAccessor,
    IRedisCacheService redisCacheService) : IAuthenticationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserSubscriptionService _userSubscriptionService = userSubscriptionService;
    private readonly IEffectiveEntitlementService _effectiveEntitlementService = effectiveEntitlementService;
    private readonly IJsonWebToken _jsonWebToken = jsonWebToken;
    private readonly IMapper _mapper = mapper;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;

    public async Task<CurrentUserProfile> GetCurrentUserProfileAsync()
    {
        string userId = _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value
            ?? throw new UnauthorizedCustomException("User is not authenticated.");

        User user = await _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId && x.Status == UserStatus.Active)
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Id)
                .Include(x => x.Role))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found user UserId");

        if (user.Role == UserRole.Artist)
        {
            string artistId = await _unitOfWork.GetCollection<Artist>()
                .Find(x => x.UserId == userId)
                .Project(x => x.Id)
                .FirstOrDefaultAsync();
            return new CurrentUserProfile
            {
                UserId = user.Id,
                Role = user.Role,
                ArtistId = artistId,
            };
        }
        else if (user.Role == UserRole.Listener)
        {
            string listenerId = await _unitOfWork.GetCollection<Listener>()
                .Find(x => x.UserId == userId)
                .Project(x => x.Id)
                .FirstOrDefaultAsync();

            return new CurrentUserProfile
            {
                UserId = user.Id,
                Role = user.Role,
                ListenerId = listenerId,
            };
        }
        else
        {
            return new CurrentUserProfile
            {
                UserId = user.Id,
                Role = user.Role,
            };
        }
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private static bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }

    private async Task<bool> IsEmailExistsAsync(string email)
    {
        return await _unitOfWork.GetCollection<User>()
            .Find(u => u.Email == email)
            .Project(u => u.Email)
            .AnyAsync();
    }

    public async Task RegisterListenerAsync(ListenerRegisterRequest registerRequest)
    {
        // Kiểm tra xem email tồn tại trong database
        if (await IsEmailExistsAsync(registerRequest.Email.Trim().ToLowerInvariant()))
        {
            throw new ConflictCustomException("Email already exists.");
        }

        // Kiểm tra xem email đã có đơn đăng ký pending chưa
        string redisKey = $"listener:*:pendingRegistration";
        string[] pendingKeys = _redisCacheService.GetAllKeysByPattern(redisKey);

        foreach (string key in pendingKeys)
        {
            if (_redisCacheService.TryGetGeneric<PendingListenerRegistration>(key, out var pendingReg)
                && pendingReg != null
                && pendingReg.Email.Equals(registerRequest.Email.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictCustomException("A pendingListener registration request with this email is already pending verification.");
            }
        }

        string userId = ObjectId.GenerateNewId().ToString();

        // Tạo đối tượng pending registration
        PendingListenerRegistration pendingRegistration = new()
        {
            Id = userId,
            Email = registerRequest.Email.Trim().ToLowerInvariant(),
            PasswordHash = HashPassword(registerRequest.Password),
            FullName = registerRequest.FullName,
            BirthDate = registerRequest.BirthDate.Date,
            Gender = registerRequest.Gender,
            DisplayName = registerRequest.DisplayName,
            AvatarImage = registerRequest.AvatarImage,
            RequestedAt = HelperMethod.GetUtcPlus7TimeOffset()
        };

        // Lưu vào Redis với TTL 24 giờ (thời gian để verify OTP)
        string pendingKey = $"listener:{userId}:pendingRegistration";
        await _redisCacheService.SetGenericAsync(pendingKey, pendingRegistration, TimeSpan.FromHours(24));

        // Gửi mã OTP để xác thực email
        string otp = await GenerateAndSetOtpAsync(pendingRegistration.Email);
        BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(
            EmailTemplateType.VerifyOtp,
            pendingRegistration.Email,
            pendingRegistration.FullName,
            otp
        ));
    }

    public async Task<AuthListenerTokenResponse> LoginListenerAsync(LoginRequest loginRequest)
    {
        // Define the filter to find the artist by email
        FilterDefinitionBuilder<User> filterBuilder = Builders<User>.Filter;
        FilterDefinition<User> userFilter = Builders<User>.Filter.And(
            filterBuilder.Eq(l => l.Email, loginRequest.Email.Trim().ToLowerInvariant()),
            filterBuilder.Eq(l => l.Status, UserStatus.Active),
            filterBuilder.Eq(l => l.IsLinkedWithGoogle, false),
            filterBuilder.Eq(l => l.Role, UserRole.Listener)
        );
        ProjectionDefinition<UserProjection> listenerUserProjection = Builders<UserProjection>.Projection
            .Include(lp => lp.Id)
            .Include(lp => lp.ListenerProjection!.Id)
            .Include(lp => lp.Role)
            .Include(lp => lp.PasswordHash)
            .Include(lp => lp.ListenerProjection!.AvatarImage);

        UserProjection userListener = await _unitOfWork.GetCollection<User>().Aggregate()
            .Match(userFilter)
            .Lookup<User, Listener, UserProjection>(
                _unitOfWork.GetCollection<Listener>(),
                user => user.Id,
                listener => listener.UserId,
                userProjection => userProjection.ListenerProjection
            )
            .Unwind(u => u.ListenerProjection, new AggregateUnwindOptions<UserProjection>
            {
                PreserveNullAndEmptyArrays = true
            })
            .Project<UserProjection>(listenerUserProjection)
            .FirstOrDefaultAsync() ?? throw new BadRequestCustomException("Invalid email or password.");

        // Kiểm tra mật khẩu
        if (!VerifyPassword(loginRequest.Password, userListener.PasswordHash!))
        {
            throw new BadRequestCustomException("Invalid email or password.");
        }

        // Tạo claims
        IEnumerable<Claim> claims =
        [
            new Claim("userId", userListener.Id),
            new Claim("listenerId",userListener.ListenerProjection!.Id),
            new Claim(ClaimTypes.Role, userListener.Role.ToString()),
            new Claim("avatarImage", userListener.ListenerProjection!.AvatarImage ?? string.Empty),
        ];

        // Tạo access token
        AccessTokenResponse token = await _jsonWebToken.GenerateAccessTokenAsync(claims);

        CookieOptions cookieOptions = new()
        {
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            MaxAge = TimeSpan.FromDays(7)
        };

        _httpContextAccessor.HttpContext?.Response.Cookies.Append("refresh_token", token.RefreshToken, cookieOptions);

        return new AuthListenerTokenResponse()
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            UserId = userListener.Id,
            ListenerId = userListener.ListenerProjection.Id,
            Role = userListener.Role,
            AvatarImage = userListener.ListenerProjection!.AvatarImage ?? string.Empty,
        };
    }

    public async Task RegisterArtistAsync(ArtistRegisterRequest registerRequest)
    {
        // Kiểm tra xem email tồn tại trong database
        if (await IsEmailExistsAsync(registerRequest.Email.Trim().ToLowerInvariant()))
        {
            throw new ConflictCustomException("Email already exists.");
        }

        // Kiểm tra xem email đã có đơn đăng ký pending chưa
        string redisKey = $"artist:*:pendingRegistration";
        string[] pendingKeys = _redisCacheService.GetAllKeysByPattern(redisKey);

        foreach (string key in pendingKeys)
        {
            if (_redisCacheService.TryGetGeneric<PendingArtistRegistrationRequest>(key, out var pendingReg)
                && pendingReg != null
                && pendingReg.Email.Equals(registerRequest.Email.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictCustomException("An artist registration request with this email is already pending approval.");
            }
        }

        string userId = ObjectId.GenerateNewId().ToString();
        List<ArtistMember> artistMembers = _mapper.Map<List<ArtistMember>>(registerRequest.Members);

        // Automatically add the registering user as the leader
        ArtistMember leaderMember = new()
        {
            FullName = registerRequest.FullName,
            Email = registerRequest.Email.Trim().ToLowerInvariant(),
            PhoneNumber = registerRequest.PhoneNumber,
            Gender = registerRequest.Gender,
            IsLeader = true
        };

        // Add leader to the beginning of the members list
        artistMembers.Insert(0, leaderMember);

        // Tạo đối tượng pending registration
        PendingArtistRegistrationRequest pendingRegistration = new()
        {
            UserId = userId,
            Email = registerRequest.Email.Trim().ToLowerInvariant(),
            PasswordHash = HashPassword(registerRequest.Password),
            FullName = registerRequest.FullName,
            BirthDate = HelperMethod.ConvertDateTimeToUtcPlus7TimeOffset(registerRequest.BirthDate.Date),
            Gender = registerRequest.Gender,
            PhoneNumber = registerRequest.PhoneNumber,
            StageName = registerRequest.StageName,
            StageNameUnsigned = HelperMethod.ToUnsigned(registerRequest.StageName),
            ArtistType = registerRequest.ArtistType,
            AvatarImage = registerRequest.AvatarImage,
            Members = artistMembers,
            IdentityCard = new IdentityCard
            {
                Number = registerRequest.IdentityCard.Number,
                FullName = registerRequest.IdentityCard.FullName,
                DateOfBirth = HelperMethod.ConvertDateTimeToUtcPlus7TimeOffset(registerRequest.IdentityCard.DateOfBirth.Date),
                Gender = registerRequest.IdentityCard.Gender,
                PlaceOfOrigin = registerRequest.IdentityCard.PlaceOfOrigin,
                Nationality = registerRequest.IdentityCard.Nationality,
                PlaceOfResidence = registerRequest.IdentityCard.PlaceOfResidence,
                FrontImage = registerRequest.IdentityCard.FrontImage,
                BackImage = registerRequest.IdentityCard.BackImage,
                ValidUntil = HelperMethod.ConvertDateTimeToUtcPlus7TimeOffset(registerRequest.IdentityCard.ValidUntil.Date),
            },
            RequestedAt = HelperMethod.GetUtcPlus7TimeOffset()
        };

        // Lưu vào Redis với TTL 7 ngày (thời gian để moderator duyệt)
        string pendingKey = $"artist:{userId}:pendingRegistration";
        await _redisCacheService.SetGenericAsync(pendingKey, pendingRegistration, TimeSpan.FromDays(7));

        // Gửi thông báo email
        BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(
            EmailTemplateType.RegisterNotification,
            pendingRegistration.Email,
            pendingRegistration.FullName,
            pendingRegistration.Email
        ));
    }

    public async Task<AuthArtistTokenResponse> LoginArtistAsync(LoginRequest loginRequest)
    {
        // Define the filter to find the artist by email
        FilterDefinitionBuilder<User> filterBuilder = Builders<User>.Filter;
        FilterDefinition<User> userFilter = Builders<User>.Filter.And(
            filterBuilder.Eq(a => a.Email, loginRequest.Email.Trim().ToLowerInvariant()),
            //filterBuilder.Eq(a => a.Status, UserStatus.Active),
            filterBuilder.Eq(a => a.IsLinkedWithGoogle, false),
            filterBuilder.Eq(a => a.Role, UserRole.Artist)
        );

        ProjectionDefinition<UserProjection> artistUserProjection = Builders<UserProjection>.Projection
            .Include(ap => ap.Id)
            .Include(ap => ap.ArtistProjection!.Id)
            .Include(ap => ap.Role)
            .Include(ap => ap.Status)
            .Include(ap => ap.PasswordHash)
            .Include(ap => ap.ArtistProjection!.AvatarImage);

        UserProjection userArtist = await _unitOfWork.GetCollection<User>().Aggregate()
            .Match(userFilter)
            .Lookup<User, Artist, UserProjection>(
                _unitOfWork.GetCollection<Artist>(),
                user => user.Id,
                artist => artist.UserId,
                userProjection => userProjection.ArtistProjection
            )
            .Unwind(u => u.ArtistProjection, new AggregateUnwindOptions<UserProjection>
            {
                PreserveNullAndEmptyArrays = true
            })
            .Project<UserProjection>(artistUserProjection)
            .FirstOrDefaultAsync() ?? throw new BadRequestCustomException("Invalid email or password.");

        // Kiểm tra mật khẩu
        if (!VerifyPassword(loginRequest.Password, userArtist.PasswordHash!))
        {
            throw new BadRequestCustomException("Invalid email or password.");
        }

        switch (userArtist.Status)
        {
            case UserStatus.Inactive:
                throw new UnauthorizedCustomException("Your account is not active.");
            case UserStatus.Banned:
                throw new UnauthorizedCustomException("Your account has been banned.");
            // Mặc định là Active
            case UserStatus.Active:
                break;
            default:
                throw new BadRequestCustomException("Invalid user status.");
        }

        // Tạo claims
        IEnumerable<Claim> claims =
        [
            new Claim("userId", userArtist.Id),
            new Claim("artistId",userArtist.ArtistProjection!.Id),
            new Claim(ClaimTypes.Role, userArtist.Role.ToString()),
            new Claim("avatarImage", userArtist.ArtistProjection!.AvatarImage ?? string.Empty),
        ];

        // Tạo access token
        AccessTokenResponse token = await _jsonWebToken.GenerateAccessTokenAsync(claims);

        CookieOptions cookieOptions = new()
        {
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            MaxAge = TimeSpan.FromDays(7)
        };

        _httpContextAccessor.HttpContext?.Response.Cookies.Append("refresh_token", token.RefreshToken, cookieOptions);

        return new AuthArtistTokenResponse()
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            UserId = userArtist.Id,
            ArtistId = userArtist.ArtistProjection.Id,
            Role = userArtist.Role,
            AvatarImage = userArtist.ArtistProjection.AvatarImage ?? string.Empty,
        };
    }

    public async Task<AuthModeratorTokenResponse> LoginModeratorAsync(LoginRequest loginRequest)
    {
        User moderator = await _unitOfWork.GetCollection<User>()
            .Find(u => u.Email == loginRequest.Email.Trim().ToLowerInvariant()
                && u.Role == UserRole.Moderator)
            .Project<User>(Builders<User>.Projection.Include(x => x.Id)
                .Include(x => x.Role)
                .Include(x => x.Status)
                .Include(x => x.PasswordHash))
            .FirstOrDefaultAsync() ?? throw new BadRequestCustomException("Invalid email or password.");

        // Kiểm tra mật khẩu
        if (!VerifyPassword(loginRequest.Password, moderator.PasswordHash!))
        {
            throw new BadRequestCustomException("Invalid email or password.");
        }

        switch (moderator.Status)
        {
            case UserStatus.Inactive:
                throw new UnauthorizedCustomException("Your account is not active.");
            case UserStatus.Banned:
                throw new UnauthorizedCustomException("Your account has been banned.");
            // Mặc định là Active
            case UserStatus.Active:
                break;
            default:
                throw new BadRequestCustomException("Invalid user status.");
        }

        // Tạo claims
        IEnumerable<Claim> claims =
        [
            new Claim("userId", moderator.Id),
            new Claim(ClaimTypes.Role, moderator.Role.ToString()),
        ];

        // Tạo access token
        AccessTokenResponse token = await _jsonWebToken.GenerateAccessTokenAsync(claims);

        CookieOptions cookieOptions = new()
        {
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            MaxAge = TimeSpan.FromDays(7)
        };

        _httpContextAccessor.HttpContext?.Response.Cookies.Append("refresh_token", token.RefreshToken, cookieOptions);

        return new AuthModeratorTokenResponse()
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            UserId = moderator.Id,
            Role = moderator.Role,
        };
    }

    public async Task<AuthAdminTokenResponse> LoginAdminAsync(LoginRequest loginRequest)
    {
        User admin = await _unitOfWork.GetCollection<User>()
            .Find(u => u.Email == loginRequest.Email.Trim().ToLowerInvariant()
                && u.Role == UserRole.Admin)
            .Project<User>(Builders<User>.Projection.Include(x => x.Id)
                .Include(x => x.Role)
                .Include(x => x.Status)
                .Include(x => x.PasswordHash))
            .FirstOrDefaultAsync() ?? throw new BadRequestCustomException("Invalid email or password.");

        // Kiểm tra mật khẩu
        if (!VerifyPassword(loginRequest.Password, admin.PasswordHash!))
        {
            throw new BadRequestCustomException("Invalid email or password.");
        }

        switch (admin.Status)
        {
            case UserStatus.Inactive:
                throw new UnauthorizedCustomException("Your account is not active.");
            case UserStatus.Banned:
                throw new UnauthorizedCustomException("Your account has been banned.");
            // Mặc định là Active
            case UserStatus.Active:
                break;
            default:
                throw new BadRequestCustomException("Invalid user status.");
        }

        // Tạo claims
        IEnumerable<Claim> claims =
        [
            new Claim("userId", admin.Id),
            new Claim(ClaimTypes.Role, admin.Role.ToString()),
        ];

        // Tạo access token
        AccessTokenResponse token = await _jsonWebToken.GenerateAccessTokenAsync(claims);

        CookieOptions cookieOptions = new()
        {
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            MaxAge = TimeSpan.FromDays(7)
        };

        _httpContextAccessor.HttpContext?.Response.Cookies.Append("refresh_token", token.RefreshToken, cookieOptions);

        return new AuthAdminTokenResponse()
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            UserId = admin.Id,
            Role = admin.Role,
        };
    }

    public async Task<AccessTokenResponse> RefreshNewTokenAsync()
    {
        string refreshToken = _httpContextAccessor.HttpContext?.Request.Cookies["refresh_token"]
                              ?? throw new BadRequestCustomException("Refresh token is missing.");

        return await _jsonWebToken.GenerateRefreshTokenAsync(refreshToken);
    }

    public async Task LogoutAsync()
    {
        string userId = _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value
                        ?? throw new UnauthorizedCustomException("You have not login yet.");

        await _jsonWebToken.RevokeToken(userId);
    }

    private async Task<string> GenerateAndSetOtpAsync(string email)
    {
        // Tạo mã OTP gồm 6 chữ số ngẫu nhiên
        const string DIGITS = "0123456789";
        Random random = new();
        StringBuilder otpBuilder = new(6);

        // Tạo 6 chữ số ngẫu nhiên
        for (int i = 0; i < 6; i++)
        {
            otpBuilder.Append(DIGITS[random.Next(DIGITS.Length)]);
        }

        string otpCode = otpBuilder.ToString();

        // Tạo Redis key cho OTP
        string redisKey = $"otp:{email}";

        // Lưu mã OTP vào Redis với thời hạn 5 phút
        await _redisCacheService.SetStringAsync(redisKey, otpCode, TimeSpan.FromMinutes(5));

        return otpCode;
    }

    // Verify OTP
    public async Task VerifyOtpAsync(string email, string providedOtp)
    {
        string redisOtpKey = $"otp:{email.ToLowerInvariant()}";
        string? storedOtp = await _redisCacheService.GetStringAsync(redisOtpKey);

        if (string.IsNullOrEmpty(storedOtp))
        {
            throw new NotFoundCustomException("OTP is expired or not found"); // OTP expired or not found
        }

        bool isValid = storedOtp.Equals(providedOtp, StringComparison.OrdinalIgnoreCase);

        if (!isValid)
        {
            throw new ConflictCustomException("Invalid OTP provided.");
        }

        // Tìm kiếm pending pendingListener registration
        string[] listenerKeys = _redisCacheService.GetAllKeysByPattern("listener:*:pendingRegistration");

        // Tìm pending pendingListener registration
        foreach (string key in listenerKeys)
        {
            if (_redisCacheService.TryGetGeneric<PendingListenerRegistration>(key, out PendingListenerRegistration? pendingListener)
                && pendingListener != null
                && pendingListener.Email.Equals(email.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
            {
                // Tạo pendingListener và user mới từ pending registration
                await _unitOfWork.ExecuteInTransactionAsync(async session =>
                {
                    // Tạo pendingListener và user từ pending registration
                    User user = new()
                    {
                        Id = pendingListener.Id,
                        Email = pendingListener.Email,
                        PasswordHash = pendingListener.PasswordHash,
                        FullName = pendingListener.FullName,
                        BirthDate = pendingListener.BirthDate,
                        Gender = pendingListener.Gender,
                        Role = UserRole.Listener,
                        Status = UserStatus.Active, // Active ngay sau khi verify OTP
                        IsLinkedWithGoogle = false,
                        CreatedAt = HelperMethod.GetUtcPlus7TimeOffset()
                    };

                    Listener listener = new()
                    {
                        UserId = pendingListener.Id,
                        DisplayName = pendingListener.DisplayName,
                        DisplayNameUnsigned = pendingListener.DisplayNameUnsigned,
                        AvatarImage = pendingListener.AvatarImage,
                        Email = pendingListener.Email,
                        Restriction = new Restriction
                        {
                            Type = RestrictionType.None,
                        },
                        CreatedAt = HelperMethod.GetUtcPlus7TimeOffset()
                    };

                    await _unitOfWork.GetCollection<User>().InsertOneAsync(session, user);
                    await _unitOfWork.GetCollection<Listener>().InsertOneAsync(session, listener);

                    // Tạo mới UserSubscription với gói Free
                    await _userSubscriptionService.CreateUserSubscriptionAsync(session, pendingListener.Id, string.Empty, HelperMethod.GetUtcPlus7TimeOffset());

                    // Xây dựng quyền lợi mặc định cho Listener (gói Free)
                    await _effectiveEntitlementService.BuildFreeTierAsync(session, pendingListener.Id, UserRole.Listener);
                });

                // Xóa pending registration sau khi tạo thành công
                if (key != null)
                {
                    await _redisCacheService.RemoveAsync(key);
                }

                // Remove OTP after successful verification
                await _redisCacheService.RemoveAsync(redisOtpKey);

                return;
            }
        }

        
    }

    public async Task ResendOtpAsync(string email)
    {
        // Tìm kiếm trong pending registrations trước
        string[] listenerKeys = _redisCacheService.GetAllKeysByPattern("listener:*:pendingRegistration");
        string[] artistKeys = _redisCacheService.GetAllKeysByPattern("artist:*:pendingRegistration");

        string? fullName = null;
        string normalizedEmail = email.ToLowerInvariant();

        // Tìm trong pending pendingListener registrations
        foreach (string key in listenerKeys)
        {
            if (_redisCacheService.TryGetGeneric<PendingListenerRegistration>(key, out var listener)
                && listener != null
                && listener.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                fullName = listener.FullName;
                break;
            }
        }

        // Nếu không tìm thấy pendingListener, tìm trong pending artist registrations
        if (fullName == null)
        {
            foreach (string key in artistKeys)
            {
                if (_redisCacheService.TryGetGeneric<PendingArtistRegistrationRequest>(key, out var artist)
                    && artist != null
                    && artist.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase))
                {
                    fullName = artist.FullName;
                    break;
                }
            }
        }

        // Nếu không tìm thấy trong pending registrations, tìm trong database
        if (fullName == null)
        {
            User user = await _unitOfWork.GetCollection<User>()
                .Find(u => u.Email == normalizedEmail && u.Status == UserStatus.Inactive)
                .Project<User>(Builders<User>.Projection
                    .Include(x => x.Email)
                    .Include(x => x.FullName))
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("User not found.");

            fullName = user.FullName;
        }

        if (string.IsNullOrEmpty(fullName))
        {
            throw new NotFoundCustomException("User registration not found.");
        }

        // Gửi lại mã OTP
        string otp = await GenerateAndSetOtpAsync(normalizedEmail);
        BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(
                EmailTemplateType.VerifyOtp,
                normalizedEmail,
                fullName,
                otp
        ));
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest forgotPasswordRequest)
    {
        string normalizedEmail = forgotPasswordRequest.Email.Trim().ToLowerInvariant();

        // Tìm user trong database với email và status Active
        User user = await _unitOfWork.GetCollection<User>()
            .Find(u => u.Email == normalizedEmail && u.Status == UserStatus.Active)
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Email)
                .Include(x => x.FullName))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("User with this email not found or account is not active.");

        // Tạo OTP đặc biệt cho reset password với thời hạn 10 phút
        string otpCode = await GenerateResetPasswordOtpAsync(normalizedEmail);

        // Gửi email với OTP reset password
        BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(
            EmailTemplateType.ResetPasswordOtp,
            user.Email,
            user.FullName,
            otpCode
        ));
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest)
    {
        string normalizedEmail = resetPasswordRequest.Email.Trim().ToLowerInvariant();

        // Verify OTP for password reset
        string resetOtpKey = $"reset_password_otp:{normalizedEmail}";
        string? storedOtp = await _redisCacheService.GetStringAsync(resetOtpKey);

        if (string.IsNullOrEmpty(storedOtp))
        {
            throw new NotFoundCustomException("Reset password OTP is expired or not found");
        }

        if (!storedOtp.Equals(resetPasswordRequest.OtpCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictCustomException("Invalid OTP code provided.");
        }

        // Tìm user và cập nhật password
        User user = await _unitOfWork.GetCollection<User>()
            .Find(u => u.Email == normalizedEmail && u.Status == UserStatus.Active)
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Id)
                .Include(x => x.Email))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("User with this email not found or account is not active.");

        // Cập nhật password
        string newPasswordHash = HashPassword(resetPasswordRequest.NewPassword);
        
        UpdateDefinition<User> updateDefinition = Builders<User>.Update
            .Set(u => u.PasswordHash, newPasswordHash)
            .Set(u => u.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

        await _unitOfWork.GetCollection<User>().UpdateOneAsync(
            u => u.Id == user.Id, 
            updateDefinition
        );

        // Xóa OTP sau khi reset thành công
        await _redisCacheService.RemoveAsync(resetOtpKey);

        // Có thể gửi email xác nhận reset password thành công (optional)
        // BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(...));
    }

    private async Task<string> GenerateResetPasswordOtpAsync(string email)
    {
        // Tạo mã OTP gồm 6 chữ số ngẫu nhiên
        const string DIGITS = "0123456789";
        Random random = new();
        StringBuilder otpBuilder = new(6);

        // Tạo 6 chữ số ngẫu nhiên
        for (int i = 0; i < 6; i++)
        {
            otpBuilder.Append(DIGITS[random.Next(DIGITS.Length)]);
        }

        string otpCode = otpBuilder.ToString();

        // Tạo Redis key riêng cho reset password OTP
        string redisKey = $"reset_password_otp:{email}";

        // Lưu mã OTP vào Redis với thời hạn 10 phút (dài hơn OTP thông thường)
        await _redisCacheService.SetStringAsync(redisKey, otpCode, TimeSpan.FromMinutes(10));

        return otpCode;
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest changePasswordRequest)
    {
        // Lấy thông tin user hiện tại từ token
        string userId = _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value
            ?? throw new UnauthorizedCustomException("User is not authenticated.");

        // Tìm user trong database
        User user = await _unitOfWork.GetCollection<User>()
            .Find(u => u.Id == userId && u.Status == UserStatus.Active)
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Id)
                .Include(x => x.Email)
                .Include(x => x.PasswordHash))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("User not found or account is not active.");

        // Verify current password
        if (!VerifyPassword(changePasswordRequest.CurrentPassword, user.PasswordHash!))
        {
            throw new BadRequestCustomException("Current password is incorrect.");
        }

        // Hash new password
        string newPasswordHash = HashPassword(changePasswordRequest.NewPassword);

        // Update password in database
        UpdateDefinition<User> updateDefinition = Builders<User>.Update
            .Set(u => u.PasswordHash, newPasswordHash)
            .Set(u => u.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

        await _unitOfWork.GetCollection<User>().UpdateOneAsync(
            u => u.Id == userId,
            updateDefinition
        );

        // Optional: Send email notification about password change
        // BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(...));
    }
}
