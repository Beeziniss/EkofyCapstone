using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.ArtistPackages
{
    public interface IArtistPackageService
    {
        IQueryable<ArtistPackage> GetArtistPackages();
        Task ChangeArtistPackageStatusAsync(UpdateStatusArtistPackageRequest updateStatusRequest);
        Task CreateArtistPackageAsync(CreateArtistPackageRequest createRequest);
        Task UpdateArtistPackageAsync(UpdateArtistPackageRequest updateRequest);
        Task ApproveArtistPackageAsync(string id);
        Task DeleteArtistPackageAsync(string id);

        // Phương thức mới cho chức năng Redis
        Task<PaginatedData<PendingArtistPackageResponse>> GetPendingArtistPackagesAsync(int pageNumber = 1, int pageSize = 20);
        Task RejectArtistPackageAsync(string id);
    }
}
