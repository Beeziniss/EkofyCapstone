using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver
{
    public class TopTrackInfoType : ObjectType<TopTrackInfo>
    {
        protected override void Configure(IObjectTypeDescriptor<TopTrackInfo> descriptor)
        {
            descriptor.Name("TopTrackInfo");

            // Định nghĩa field track với resolver
            descriptor
                .Field("track")
                .ResolveWith<TrackInfoResolver>(r => r.GetTrackAsync(default!, default!))
                .Type<ObjectType<Track>>();
        }
    }
    public class TrackInfoResolver
    {
        public async Task<Track?> GetTrackAsync(
            [Parent] TopTrackInfo trackInfo,
            [Service] IUnitOfWork unitOfWork)
        {
            return await unitOfWork.GetCollection<Track>()
                .Find(t => t.Id == trackInfo.TrackId)
                .FirstOrDefaultAsync();
        }
    }
}
