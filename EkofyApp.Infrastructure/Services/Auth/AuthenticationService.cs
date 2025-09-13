using AutoMapper;
using CloudinaryDotNet.Actions;
using EkofyApp.Application.Models.Auth.Admins;
using EkofyApp.Application.Models.Auth.Artists;
using EkofyApp.Application.Models.Auth.Listeners;
using EkofyApp.Application.Models.Auth.Moderators;
using EkofyApp.Application.Models.Projections;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Authentication;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;
using LoginRequest = EkofyApp.Application.Models.Auth.LoginRequest;

namespace EkofyApp.Infrastructure.Services.Auth;
public sealed class AuthenticationService(IUnitOfWork unitOfWork, IUserSubscriptionService userSubscriptionService, IEffectiveEntitlementService effectiveEntitlementService, IJsonWebToken jsonWebToken, IMapper mapper) : IAuthenticationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserSubscriptionService _userSubscriptionService = userSubscriptionService;
    private readonly IEffectiveEntitlementService _effectiveEntitlementService = effectiveEntitlementService;
    private readonly IJsonWebToken _jsonWebToken = jsonWebToken;
    private readonly IMapper _mapper = mapper;

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
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // Kiểm tra xem email tồn tại
            if (await IsEmailExistsAsync(registerRequest.Email.Trim().ToLowerInvariant()))
            {
                throw new ConflictCustomException("Email already exists.");
            }

            string userId = ObjectId.GenerateNewId().ToString();

            // Tạo người dùng mới
            User user = new()
            {
                Id = userId,
                Email = registerRequest.Email.Trim().ToLowerInvariant(),
                PasswordHash = HashPassword(registerRequest.Password),
                FullName = registerRequest.FullName,
                BirthDate = registerRequest.BirthDate.Date,
                Gender = registerRequest.Gender,
                Role = UserRole.Listener, // Mặc định là Listener
                IsLinkedWithGoogle = false,
            };

            Listener listener = new()
            {
                UserId = userId,
                DisplayName = registerRequest.DisplayName,
                Email = registerRequest.Email.Trim().ToLowerInvariant(),
                Restriction = new Restriction
                {
                    Type = RestrictionType.None, // Mặc định không có hạn chế
                },
            };

            // Lưu người dùng và artist vào cơ sở dữ liệu
            await _unitOfWork.GetCollection<User>().InsertOneAsync(session, user);
            await _unitOfWork.GetCollection<Listener>().InsertOneAsync(session, listener);

            // Tạo mới UserSubscription với gói Free
            await _userSubscriptionService.CreateUserSubscriptionAsync(session, string.Empty, HelperMethod.GetUtcPlus7TimeOffset());

            // Xây dựng quyền lợi mặc định cho Listener (gói Free)
            await _effectiveEntitlementService.BuildFreeTierAsync(session, userId, UserRole.Listener);
        });
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
            .Include(lp => lp.PasswordHash);

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
        ];

        // Tạo access token
        string accessToken = _jsonWebToken.GenerateAccessToken(claims);

        return new AuthListenerTokenResponse()
        {
            AccessToken = accessToken,
            UserId = userListener.Id,
            ListenerId = userListener.ListenerProjection.Id,
            Role = userListener.Role,
        };
    }

    public async Task RegisterArtistAsync(ArtistRegisterRequest registerRequest)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // Kiểm tra xem email tồn tại
            if (await IsEmailExistsAsync(registerRequest.Email.Trim().ToLowerInvariant()))
            {
                throw new ConflictCustomException("Email already exists.");
            }

            string userId = ObjectId.GenerateNewId().ToString();

            // Tạo người dùng mới
            User user = new()
            {
                Id = userId,
                Email = registerRequest.Email.Trim().ToLowerInvariant(),
                PasswordHash = HashPassword(registerRequest.Password),
                FullName = registerRequest.FullName,
                BirthDate = HelperMethod.ConvertDateTimeToUtcPlus7TimeOffset(registerRequest.BirthDate.Date),
                Gender = registerRequest.Gender,
                PhoneNumber = registerRequest.PhoneNumber,
                Role = UserRole.Artist,
                IsLinkedWithGoogle = false,
            };

            List<ArtistMember> artistMembers = _mapper.Map<List<ArtistMember>>(registerRequest.Members);
            if (!registerRequest.IsLegalRepresentative)
            {
                artistMembers.Add(new ArtistMember
                {
                    FullName = registerRequest.IdentityCard.FullName,
                    Email = registerRequest.Email.Trim().ToLowerInvariant(),
                    PhoneNumber = registerRequest.PhoneNumber,
                    IsLeader = true,
                    Gender = registerRequest.IdentityCard.Gender,
                });
            }

            Artist artist = new()
            {
                UserId = userId,
                StageName = registerRequest.StageName,
                Email = registerRequest.Email.Trim().ToLowerInvariant(),
                Restriction = new Restriction
                {
                    Type = RestrictionType.None, // Mặc định không có hạn chế
                },

                ArtistType = registerRequest.ArtistType,
                Members = artistMembers,

                // TODO: Cập nhật thông tin thật nha và tạm thời không validate IdentityCard
                // Vì đang giả định thông tin này là đúng và được xử lý từ phía client có dùng AI FPT
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
                }
            };

            // Lưu người dùng và artist vào cơ sở dữ liệu
            await _unitOfWork.GetCollection<User>().InsertOneAsync(user);
            await _unitOfWork.GetCollection<Artist>().InsertOneAsync(artist);

            // Tạo mới UserSubscription với gói Free
            await _userSubscriptionService.CreateUserSubscriptionAsync(session, string.Empty, HelperMethod.GetUtcPlus7TimeOffset());

            // Xây dựng quyền lợi mặc định cho Artist (gói Free)
            await _effectiveEntitlementService.BuildFreeTierAsync(session, userId, UserRole.Artist);
        });
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
            .Include(ap => ap.PasswordHash);

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
        ];

        // Tạo access token
        string accessToken = _jsonWebToken.GenerateAccessToken(claims);

        return new AuthArtistTokenResponse()
        {
            AccessToken = accessToken,
            UserId = userArtist.Id,
            ArtistId = userArtist.ArtistProjection.Id,
            Role = userArtist.Role,
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
        string accessToken = _jsonWebToken.GenerateAccessToken(claims);

        return new AuthModeratorTokenResponse()
        {
            AccessToken = accessToken,
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
        string accessToken = _jsonWebToken.GenerateAccessToken(claims);

        return new AuthAdminTokenResponse()
        {
            AccessToken = accessToken,
            UserId = admin.Id,
            Role = admin.Role,
        };
    }
}
