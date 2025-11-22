namespace EkofyApp.Application.Models.Albums;

public sealed record class AddTrackToAlbumRequest
{
    public string TrackId { get; init; } = null!; // ID of the track to be added to the album
    public string? AlbumId { get; init; } // Unique identifier for the album to which the track will be added
    public string? AlbumName { get; init; } // Name of the album, used for creating a new album if it doesn't exist
}