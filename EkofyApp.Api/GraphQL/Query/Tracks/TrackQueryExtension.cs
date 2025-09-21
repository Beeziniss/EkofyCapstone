using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Tracks;

public class TrackQueryExtension : ObjectTypeExtension<TrackQuery>
{
    protected override void Configure(IObjectTypeDescriptor<TrackQuery> descriptor)
    {
        descriptor.Field(x => x.GetTracks())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting<Track>();

        descriptor.Field(x => x.GetPendingTrackUploadRequestsAsync())
            .Authorize(roles: HelperRoleBase.ModeratorRoles)
            .UseProjection()
            .UseFiltering();

        descriptor.Field(x => x.GetMetadataTrackUploadRequestAsync(default!))
            .Authorize(roles: HelperRoleBase.ModeratorRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting<Track>();

        descriptor.Field(x => x.GetOriginalFileTrackUploadRequest(default!))
            .Authorize(roles: HelperRoleBase.ModeratorRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting<Track>();
    }
}