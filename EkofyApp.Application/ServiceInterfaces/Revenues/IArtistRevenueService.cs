using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Revenues;
public interface IArtistRevenueService
{
    Task<ArtistRevenue> ComputeArtistRevenueByArtistIdAsync(string artistId);
    IQueryable<ArtistRevenue> GetArtistRevenues();
}
