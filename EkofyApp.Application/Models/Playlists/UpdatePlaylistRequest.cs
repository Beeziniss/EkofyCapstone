namespace EkofyApp.Application.Models.Playlists;
public sealed record class UpdatePlaylistRequest
{
    public string PlaylistId { get; init; } = null!; // ID of the playlist to be updated
    public string? Name { get; init; } // DisplayName of the playlist, e.g., "Chill Vibes"
    public string? Description { get; init; }// PackageDescription of the playlist, e.g., "A collection of relaxing tunes"
    public string? CoverImage { get; init; } // URL to the playlist's cover image, e.g., "https://example.com/cover.jpg"
    public bool? IsPublic { get; init; } // Indicates if the playlist is public or private, e.g., true for public
}
