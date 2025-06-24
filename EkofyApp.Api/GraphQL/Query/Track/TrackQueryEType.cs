namespace EkofyApp.Api.GraphQL.Query.Track;

public class TrackQueryEType : ObjectTypeExtension<TrackQuery>
{
    protected override void Configure(IObjectTypeDescriptor<TrackQuery> descriptor)
    {
        descriptor.Field(x => x.GetCustomTrackResponseDto())
            .UseFiltering()
            .UseSorting();
    }
}