namespace EkofyApp.Application.Models.Albums;

public sealed record class RemoveTrackFromAlbumRequest
{
    public string TrackId { get; init; } = null!; // ID of the track to be removed from the album
    public string AlbumId { get; init; } = null!; // Unique identifier for the album from which the track will be removed
}