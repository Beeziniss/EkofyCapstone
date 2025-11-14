using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver
{
    [ExtendObjectType(typeof(Request))]
    public sealed class RequestResolver
    {
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Listener> GetRequestor([Parent] Request request, [Service] IUnitOfWork unitOfWork)
        {
            return unitOfWork.GetCollection<Listener>().AsQueryable().Where(x => x.UserId == request.RequestUserId);
        }

        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Artist> GetArtist([Parent] Request request, [Service] IUnitOfWork unitOfWork)
        {
            var artist_packages = unitOfWork.GetCollection<ArtistPackage>().AsQueryable();
            var artists = unitOfWork.GetCollection<Artist>().AsQueryable();

            return from ap in artist_packages
                   join a in artists on ap.ArtistId equals a.Id
                   where ap.Id == request.PackageId
                   select a;
        }
    }
}
