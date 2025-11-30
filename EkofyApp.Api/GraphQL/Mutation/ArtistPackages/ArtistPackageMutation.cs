using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Application.ServiceInterfaces.ArtistPackages;
using EkofyApp.Domain.Utils;
using HotChocolate.Authorization;

namespace EkofyApp.Api.GraphQL.Mutation.ArtistPackages
{
    [ExtendObjectType(typeof(MutationInitialization))]
    [MutationType]
    public sealed class ArtistPackageMutation(IArtistPackageService artistPackageService)
    {
        private readonly IArtistPackageService _artistPackageService = artistPackageService;

        [AuthorizeRoles(HelperRoleBase.ArtistRoles)]
        public async Task<bool> CreateArtistPackageAsync(CreateArtistPackageRequest createRequest)
        {
            await _artistPackageService.CreateArtistPackageAsync(createRequest);
            return true;
        }

        [AuthorizeRoles(HelperRoleBase.ArtistRoles)]
        public async Task<bool> UpdateArtistPackageAsync(UpdateArtistPackageRequest updateRequest)
        {
            await _artistPackageService.UpdateArtistPackageAsync(updateRequest);
            return true;
        }

        [AuthorizeRoles(HelperRoleBase.ArtistRoles)]
        public async Task<bool> DeleteArtistPackageAsync(string artistPackageId)
        {
            await _artistPackageService.DeleteArtistPackageAsync(artistPackageId);
            return true;
        }

        [AuthorizeRoles(HelperRoleBase.ArtistRoles)]
        public async Task<bool> ChangeArtistPackageStatusAsync(UpdateStatusArtistPackageRequest updateStatusRequest)
        {
            await _artistPackageService.ChangeArtistPackageStatusAsync(updateStatusRequest);
            return true;
        }

        [AuthorizeRoles(HelperRoleBase.ArtistRoles)]
        public async Task<bool> CreateCustomArtistPackageAsync(CreateCustomArtistPackageRequest createRequest)
        {
            await _artistPackageService.CreateCustomArtistPackageAsync(createRequest);
            return true;
        }

        [AuthorizeRoles(HelperRoleBase.ArtistRoles)]
        public async Task<bool> UpdateCustomPackageAsync(UpdateCustomArtistPackageRequest updateRequest)
        {
            await _artistPackageService.UpdateCustomPackageAsync(updateRequest);
            return true;
        }

        [AuthorizeRoles(HelperRoleBase.ArtistRoles)]
        public async Task<bool> DeleteCustomArtistPackageAsync(string artistPackageId)
        {
            await _artistPackageService.DeleteCustomArtistPackageAsync(artistPackageId);
            return true;
        }
    }
}
