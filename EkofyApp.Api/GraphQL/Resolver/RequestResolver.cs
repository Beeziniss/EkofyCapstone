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
            return unitOfWork.GetCollection<Artist>().AsQueryable().Where(x => x.Id == request.ArtistId);
        }

        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ArtistPackage> GetArtistPackage([Parent] Request request, [Service] IUnitOfWork unitOfWork)
        {
            return unitOfWork.GetCollection<ArtistPackage>().AsQueryable().Where(x => x.Id == request.PackageId);
        }
    }
}
