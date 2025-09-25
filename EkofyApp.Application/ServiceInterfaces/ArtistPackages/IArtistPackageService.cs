using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.ArtistPackages
{
    public interface IArtistPackageService
    {
        IQueryable<ArtistPackage> GetArtistPackages();
        Task ChangeArtistPackageStatus(UpdateStatusArtistPackageRequest updateStatusRequest);
        Task CreateArtistPackageAsync(CreateArtistPackageRequest createRequest);
        //Task UpdateArtistPackageAsync(UpdateArtistPackageRequest updateRequest);
        Task ApproveArtistPackage(UpdateStatusArtistPackageRequest updateStatusRequest);
    }
}
