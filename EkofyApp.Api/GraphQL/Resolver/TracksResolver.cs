using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Playlists;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using HotChocolate.Data;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Track))]
public sealed class TracksResolver
{
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Artist> GetMainArtists([Parent] Track track, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Artist>().AsQueryable().Where(x => track.MainArtistIds.Contains(x.Id));
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Artist> GetFeaturedArtists([Parent] Track track, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Artist>().AsQueryable().Where(x => track.FeaturedArtistIds.Contains(x.Id));
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Category> GetCategories([Parent] Track track, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Category>().AsQueryable().Where(x => track.CategoryIds.Contains(x.Id));
    }

    public async Task<bool> CheckTrackInFavoriteAsync([Parent] Track track, [Service] ITrackService trackService)
    {
        return await trackService.CheckTrackInFavoriteAsync(track.Id);
    }
}
