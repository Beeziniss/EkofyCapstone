namespace EkofyApp.Application.Models.Playlists;
public sealed record class AddToPlaylistRequest
{
    public string TrackId { get; init; } = null!; // List of track IDs to be added to the playlist
    public string? PlaylistId { get; init; } // Unique identifier for the playlist to which tracks will be added

    public string? PlaylistName { get; init; } // DisplayName of the playlist, used for creating a new playlist if it doesn't exist
}
