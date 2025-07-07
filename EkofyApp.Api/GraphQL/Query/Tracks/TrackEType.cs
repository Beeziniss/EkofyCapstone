using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Tracks;

public class TrackEType : ObjectTypeExtension<Track>
{
    protected override void Configure(IObjectTypeDescriptor<Track> descriptor)
    {
        // You can configure the Track type here if needed
        // For example, you can ignore certain fields or add custom resolvers
        // descriptor.BindFieldsExplicitly();
        
        //descriptor.Ignore(x => x.AudioFeature); // Example of ignoring a field
        //descriptor.Ignore(x => x.AudioFingerprint);
    }
}
