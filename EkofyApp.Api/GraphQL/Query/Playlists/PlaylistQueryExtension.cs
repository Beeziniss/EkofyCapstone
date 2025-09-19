namespace EkofyApp.Api.GraphQL.Query.Playlists;

public sealed class PlaylistQueryExtension : ObjectTypeExtension<PlaylistQuery>
{
    protected override void Configure(IObjectTypeDescriptor<PlaylistQuery> descriptor)
    {
        descriptor.Field(x => x.GetPlaylists())
            .Authorize(roles: ["Listener", "Artist"]);
        //.AllowAnonymous();
    }
}
