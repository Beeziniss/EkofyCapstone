namespace EkofyApp.Api.GraphQL.Mutation.Tracks
{
    public class TrackMutationExtension : ObjectTypeExtension<TrackMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<TrackMutation> descriptor)
        {
            // Configure the TrackMutation type here if needed
            descriptor.Field(x => x.UploadTrackAsync(default!, default!))
                .Authorize(roles: "Artist");

            descriptor.Field(x => x.ApproveTrackUploadRequestAsync(default!))
                .Authorize(roles: "Moderator");

            descriptor.Field(x => x.RejectTrackUploadRequestAsync(default!))
                .Authorize(roles: "Moderator");
        }
    }
}
