using EkofyApp.Application.Models.Artists;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Artists;
public interface IArtistService
{
    Task<bool> CreateArtistAsync(CreateArtistRequest createArtistRequest);
    IQueryable<Artist> GetArtistsQueryable();
    Task UpdateArtistAsync(UpdateArtistRequest updateArtistRequest);
}
