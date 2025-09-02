using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Artists;

public sealed class ArtistService(IUnitOfWork unitOfWork) : IArtistService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

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
}
