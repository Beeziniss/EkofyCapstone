using EkofyApp.Application.ServiceInterfaces.ArtistPackages;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.ArtistPackages
{
    [ExtendObjectType(typeof(QueryInitialization))]
    [QueryType]
    public class ArtistPackageQuery(IArtistPackageService artistPackageService)
    {
        private readonly IArtistPackageService _artistPackageService = artistPackageService;

        public IQueryable<ArtistPackage> GetArtistPackages()
        {
            return _artistPackageService.GetArtistPackages();
        }
    }
}
