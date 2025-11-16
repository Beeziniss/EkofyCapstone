using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Albums;

public sealed class AlbumMutationExtension : ObjectTypeExtension<AlbumMutation>
{
    protected override void Configure(IObjectTypeDescriptor<AlbumMutation> descriptor)
    {
        descriptor.Field(x => x.CreateAlbumAsync(default!))
            .Authorize(HelperRoleBase.ArtistRolesArray); // Only artists can create albums

        descriptor.Field(x => x.AddToFavoriteAlbumAsync(default!, default!))
            .Authorize(HelperRoleBase.ListenerArtistRolesArray); // Both listeners and artists can favorite

        descriptor.Field(x => x.AddTrackToAlbumAsync(default!))
            .Authorize(HelperRoleBase.ArtistRolesArray); // Only artists can manage album tracks

        descriptor.Field(x => x.RemoveTrackFromAlbumAsync(default!))
            .Authorize(HelperRoleBase.ArtistRolesArray); // Only artists can manage album tracks

        descriptor.Field(x => x.DeleteAlbumAsync(default!))
            .Authorize(HelperRoleBase.ArtistRolesArray); // Only artists can delete albums
    }
}