using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Playlists;

public sealed class PlaylistMutationExtension : ObjectTypeExtension<PlaylistMutation>
{
    protected override void Configure(IObjectTypeDescriptor<PlaylistMutation> descriptor)
    {
        descriptor.Field(x => x.CreatePlaylistAsync(default!))
            .Authorize(HelperRoleBase.ListenerArtistRolesArray);

        descriptor.Field(x => x.UpdatePlaylistAsync(default!))
            .Authorize(HelperRoleBase.ListenerArtistRolesArray);

        descriptor.Field(x => x.AddToFavoritePlaylistAsync(default!, default!))
            .Authorize(HelperRoleBase.ListenerArtistRolesArray);

        descriptor.Field(x => x.AddToPlaylistAsync(default!))
            .Authorize(HelperRoleBase.ListenerArtistRolesArray);

        descriptor.Field(x => x.RemoveFromPlaylistAsync(default!))
            .Authorize(HelperRoleBase.ListenerArtistRolesArray);

        descriptor.Field(x => x.DeletePlaylistAsync(default!))
            .Authorize(HelperRoleBase.ListenerArtistRolesArray);
    }
}
