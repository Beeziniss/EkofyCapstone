namespace EkofyApp.Api.GraphQL.Mutation.Tracks;
public sealed class TrackMutationExtension : ObjectTypeExtension<TrackMutation>
{
    protected override void Configure(IObjectTypeDescriptor<TrackMutation> descriptor)
    {
        // Configure the TrackMutation type here if needed
        descriptor.Field(x => x.UploadTrackAsync(default!, default!, default!, default!))
            .Authorize(roles: "Artist");

        descriptor.Field(x => x.ApproveTrackUploadRequestAsync(default!, default!, default!))
            .Authorize(roles: "Moderator");

        descriptor.Field(x => x.RejectTrackUploadRequestAsync(default!, default!, default!))
            .Authorize(roles: "Moderator");
    }
}
