using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.EntityTypeExtension;

public class TrackObjectExtension : ObjectTypeExtension<Track>
{
    protected override void Configure(IObjectTypeDescriptor<Track> descriptor)
    {
        //descriptor.Field(x => x.MainArtistIds).IsProjected(true); 

        descriptor.Field(x => x.AudioFeature).Ignore();
        descriptor.Field(x => x.AudioFingerprint).Ignore();

        //descriptor.Name("Track");

        //// Let HotChocolate auto-map properties that match
        //descriptor.BindFieldsImplicitly();

        //// Custom resolver for Artist
        //descriptor.Field("artist")
        //    .Type<ArtistResponse>()
        //    .Resolve(async context =>
        //    {
        //        var track = context.Parent<Track>();
        //        var artistDataLoader = context.Service<DataLoaderCustomOneToOne<Artist>>();
        //        var mapper = context.Service<IMapper>();

        //        var artist = await artistDataLoader.LoadAsync(track.MainArtistIds, context.RequestAborted);
        //        return mapper.Map<ArtistResponse>(artist);
        //    });
    }
}
