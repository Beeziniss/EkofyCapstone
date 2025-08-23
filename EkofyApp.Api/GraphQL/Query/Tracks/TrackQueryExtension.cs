namespace EkofyApp.Api.GraphQL.Query.Tracks;

public class TrackQueryExtension : ObjectTypeExtension<TrackQuery>
{
    protected override void Configure(IObjectTypeDescriptor<TrackQuery> descriptor)
    {
        descriptor.Field(x => x.GetTracks())
            .UseProjection()
            .UseFiltering()
            .UseSorting();
    }
}