using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Infrastructure.Services.Artists;

public sealed class ArtistService(IUnitOfWork unitOfWork) : IArtistService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<bool> CreateArtistAsync(CreateArtistRequest createArtistRequest)
    {
        Artist artist = new()
        {
            UserId = createArtistRequest.UserId,
            Name = createArtistRequest.Name,
            Biography = createArtistRequest.Biography,
            IdentityCard = createArtistRequest.IdentityCard,
        };

        await _unitOfWork.GetCollection<Artist>().InsertOneAsync(artist);

        return true;
    }
}
