using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Artists;

public sealed class ArtistService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IArtistService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

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
}
