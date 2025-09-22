using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Application.ServiceInterfaces.ArtistPackages;

namespace EkofyApp.Api.GraphQL.Mutation.ArtistPackages
{
    [ExtendObjectType(typeof(MutationInitialization))]
    [MutationType]
    public sealed class ArtistPackageMutation(IArtistPackageService artistPackageService)
    {
        private readonly IArtistPackageService _artistPackageService = artistPackageService;

        public async Task<bool> CreateArtistPackageAsync(CreateArtistPackageRequest createRequest)
        {
            await _artistPackageService.CreateArtistPackageAsync(createRequest);
            return true;
        }

        //public async Task<bool> UpdateArtistPackageAsync(UpdateArtistPackageRequest updateRequest)
        //{
        //    await _artistPackageService.UpdateArtistPackageAsync(updateRequest);
        //    return true;
        //}
        public async Task<bool> ChangeArtistPackageStatusAsync(UpdateStatusArtistPackageRequest updateStatusRequest)
        {
            await _artistPackageService.ChangeArtistPackageStatus(updateStatusRequest);
            return true;
        }

        public async Task<bool> ApproveArtistPackageAsync(UpdateStatusArtistPackageRequest updateStatusRequest)
        {
            await _artistPackageService.ApproveArtistPackage(updateStatusRequest);
            return true;
        }
    }
}
