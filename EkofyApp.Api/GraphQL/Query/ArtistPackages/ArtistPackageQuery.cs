using EkofyApp.Application.ServiceInterfaces.ArtistPackages;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Authorization;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.ArtistPackages;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public class ArtistPackageQuery(IArtistPackageService artistPackageService)
{
    private readonly IArtistPackageService _artistPackageService = artistPackageService;

    //[AuthorizeRoles(HelperRoleBase.FullRoles)]
    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<ArtistPackage>]
    public IQueryable<ArtistPackage> GetArtistPackages()
    {
        return _artistPackageService.GetArtistPackages();
    }
}
