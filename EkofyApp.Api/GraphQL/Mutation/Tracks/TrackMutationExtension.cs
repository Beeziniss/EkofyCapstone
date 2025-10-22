using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Tracks;
public sealed class TrackMutationExtension : ObjectTypeExtension<TrackMutation>
{
    protected override void Configure(IObjectTypeDescriptor<TrackMutation> descriptor)
    {
        // Configure the TrackMutation type here if needed
        descriptor.Field(x => x.UploadTrackAsync(default!, default!, default!, default!))
            .Authorize(HelperRoleBase.ArtistRolesArray);

        descriptor.Field(x => x.ApproveTrackUploadRequestAsync(default!))
            .Authorize(HelperRoleBase.ModeratorRolesArray);

        descriptor.Field(x => x.RejectTrackUploadRequestAsync(default!, default!))
            .Authorize(HelperRoleBase.ModeratorRolesArray);

        descriptor.Field(x => x.UpdateFavoriteCountAsync(default!, default!, default!, default!))
            .Authorize(HelperRoleBase.ListenerArtistRolesArray);
    }
}
