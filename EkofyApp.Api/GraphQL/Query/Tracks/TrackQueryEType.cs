using EkofyApp.Application.Models.Tracks;

namespace EkofyApp.Api.GraphQL.Query.Tracks;

public class TrackQueryEType : ObjectTypeExtension<TrackQuery>
{
    protected override void Configure(IObjectTypeDescriptor<TrackQuery> descriptor)
    {
        descriptor.Field(x => x.GetCustomTrackResponseDto())
            .UseFiltering()
            .UseSorting();

        descriptor.Field(x => x.GetCustomTrackResponse(default!, default!))
            .UseProjection<TrackResponse>();

        descriptor.Field(x => x.GetTracks())
            .UseProjection<TrackResponse>();
    }
}