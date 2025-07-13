using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Artists;
public interface IArtistGraphQLService
{
    IQueryable<Artist> GetArtistsQueryable();
}
