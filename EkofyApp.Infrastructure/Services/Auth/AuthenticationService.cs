using EkofyApp.Application.Models.Auth;
using EkofyApp.Application.Models.Auth.Artists;
using EkofyApp.Application.Models.Auth.Listeners;
using EkofyApp.Application.Models.Projections;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Authentication;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;
using LoginRequest = EkofyApp.Application.Models.Auth.LoginRequest;

namespace EkofyApp.Infrastructure.Services.Auth;
public sealed class AuthenticationService(IUnitOfWork unitOfWork, IJsonWebToken jsonWebToken, IHttpContextAccessor httpContextAccessor) : IAuthenticationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IJsonWebToken _jsonWebToken = jsonWebToken;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

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
                BirthDate = registerRequest.BirthDate.Date,
                Gender = registerRequest.Gender,
                Role = UserRole.Listener, // Mặc định là Listener
                IsLinkedWithGoogle = false,
            };

            Listener listener = new()
            {
                UserId = userId,
                Name = registerRequest.Name,
                Email = registerRequest.Email.Trim().ToLowerInvariant(),
                Restriction = new Restriction
                {
                    Type = RestrictionType.None, // Mặc định không có hạn chế
                },
            };

            // Lưu người dùng và artist vào cơ sở dữ liệu
            await _unitOfWork.GetCollection<User>().InsertOneAsync(user);
            await _unitOfWork.GetCollection<Listener>().InsertOneAsync(listener);
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
                BirthDate = registerRequest.BirthDate.Date,
                Gender = registerRequest.Gender,
                Role = UserRole.Artist,
                IsLinkedWithGoogle = false,
            };

            Artist artist = new()
            {
                UserId = userId,
                Name = registerRequest.Name,
                Email = registerRequest.Email.Trim().ToLowerInvariant(),
                Restriction = new Restriction
                {
                    Type = RestrictionType.None, // Mặc định không có hạn chế
                },

                // TODO: Cập nhật thông tin thật nha và tạm thời không validate IdentityCard
                // Vì đang giả định thông tin này là đúng và được xử lý từ phía client có dùng AI FPT
                IdentityCard = new IdentityCard
                {
                    Number = registerRequest.IdentityCard.Number,
                    FullName = registerRequest.IdentityCard.FullName,
                    DateOfBirth = registerRequest.IdentityCard.DateOfBirth,
                    Gender = registerRequest.IdentityCard.Gender,
                    PlaceOfOrigin = registerRequest.IdentityCard.PlaceOfOrigin,
                    Nationality = registerRequest.IdentityCard.Nationality,
                    PlaceOfResidence = registerRequest.IdentityCard.PlaceOfResidence,
                    FrontImage = registerRequest.IdentityCard.FrontImage,
                    BackImage = registerRequest.IdentityCard.BackImage,
                }
            };

            // Lưu người dùng và artist vào cơ sở dữ liệu
            await _unitOfWork.GetCollection<User>().InsertOneAsync(user);
            await _unitOfWork.GetCollection<Artist>().InsertOneAsync(artist);
        });
    }

    public async Task<AuthArtistTokenResponse> LoginArtistAsync(LoginRequest loginRequest)
    {
        // Define the filter to find the artist by email
        FilterDefinitionBuilder<User> filterBuilder = Builders<User>.Filter;
        FilterDefinition<User> userFilter = Builders<User>.Filter.And(
            filterBuilder.Eq(a => a.Email, loginRequest.Email.Trim().ToLowerInvariant()),
            filterBuilder.Eq(a => a.Status, UserStatus.Active),
            filterBuilder.Eq(a => a.IsLinkedWithGoogle, false),
            filterBuilder.Eq(a => a.Role, UserRole.Artist)
        );

        ProjectionDefinition<UserProjection> artistUserProjection = Builders<UserProjection>.Projection
            .Include(ap => ap.Id)
            .Include(ap => ap.ArtistProjection!.Id)
            .Include(ap => ap.Role)
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
}
