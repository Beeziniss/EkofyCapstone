namespace EkofyApp.Api.GraphQL.Query.Track;

public class TrackQueryType : ObjectTypeExtension<TrackQuery>
{
    protected override void Configure(IObjectTypeDescriptor<TrackQuery> descriptor)
    {
        descriptor.Field(x => x.GetCustomTrackResponseDto(default!, default!))
            .UseFiltering()
            .UseSorting();
    }
}