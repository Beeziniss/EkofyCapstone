using EkofyApp.Application.Models.Artist;
using EkofyApp.Application.ServiceInterfaces.Artist;
using EkofyApp.Domain.Entities;
using HealthyNutritionApp.Application.Interfaces;

namespace EkofyApp.Infrastructure.Services.Artist
{
    public class ArtistService(IUnitOfWork unitOfWork) : IArtistService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<bool> CreateArtistAsync(CreateArtistRequest createArtistRequest)
        {
            Artists artist = new()
            {
                Name = createArtistRequest.Name,
                Genre = createArtistRequest.Genre,
            };

            await _unitOfWork.GetCollection<Artists>().InsertOneAsync(artist);

            return true;
        }
    }
}
