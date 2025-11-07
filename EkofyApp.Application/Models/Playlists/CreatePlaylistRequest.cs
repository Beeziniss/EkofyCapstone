namespace EkofyApp.Application.Models.Playlists;
public sealed record class CreatePlaylistRequest
{
    public string Name { get; init; } = default!; // DisplayName of the playlist, e.g., "Chill Vibes"
    public string Description { get; init; } = default!; // PackageDescription of the playlist, e.g., "A collection of relaxing tunes"
    public string? CoverImage { get; init; } // URL to the playlist's cover image, e.g., "https://example.com/cover.jpg"
    public bool IsPublic { get; init; } = false; // Indicates if the playlist is public or private, e.g., true for public
}
