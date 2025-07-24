using EkofyApp.Application.Models.Tracks;
using HotChocolate.Execution;

namespace EkofyApp.Api.GraphQL.Query.Tracks;

public class TrackQueryEType : ObjectTypeExtension<TrackQuery>
{
    protected override void Configure(IObjectTypeDescriptor<TrackQuery> descriptor)
    {
        descriptor.Field(x => x.GetTracks())
            .UseProjection()
            .UseFiltering()
            .UseSorting();

        descriptor.Field(x => x.GetTracksIe())
            .UseProjection();
    }
}