namespace EkofyApp.Api.GraphQL.Query.Tracks;

public class TrackQueryExtension : ObjectTypeExtension<TrackQuery>
{
    protected override void Configure(IObjectTypeDescriptor<TrackQuery> descriptor)
    {
        descriptor.Field(x => x.GetTracks())
            .UseProjection()
            .UseFiltering()
            .UseSorting();

        descriptor.Field(x => x.GetPendingTrackUploadRequestsAsync())
                .Authorize(roles: "Moderator");

        descriptor.Field(x => x.GetMetadataTrackUploadRequestAsync(default!))
            .Authorize(roles: "Moderator");

        descriptor.Field(x => x.GetOriginalFileTrackUploadRequest(default!))
            .Authorize(roles: "Moderator");
    }
}