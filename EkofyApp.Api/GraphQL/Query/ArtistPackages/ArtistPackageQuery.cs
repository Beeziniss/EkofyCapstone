using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Application.ServiceInterfaces.ArtistPackages;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
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

    [AuthorizeRoles(HelperRoleBase.ListenerArtistRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<ArtistPackage>]
    public IQueryable<ArtistPackage> GetArtistPackagesInConversation(string artistId)
    {
        return _artistPackageService.GetArtistPackagesInConversation(artistId);
    }

    [AuthorizeRoles(HelperRoleBase.ArtistModeratorRoles)]
    [UseFiltering]
    public async Task<PaginatedData<PendingArtistPackageResponse>> GetPendingArtistPackagesAsync(int pageNumber = 1, int pageSize = 20)
    {
        return await _artistPackageService.GetPendingArtistPackagesAsync(pageNumber, pageSize);
    }
}
