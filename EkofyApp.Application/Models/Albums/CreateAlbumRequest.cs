using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Albums;

public sealed record class CreateAlbumRequest
{
    public string Name { get; init; } = default!; // Name of the album, e.g., "My First Album"
    public string? Description { get; init; } // Description of the album, e.g., "A collection of my favorite songs"
    public AlbumType Type { get; init; } = AlbumType.Album; // Type of the album, e.g., Album, Single, EP, etc.
    public List<string> TrackIds { get; init; } = []; // List of track IDs to include in the album
    public List<ContributingArtist> ArtistInfos { get; init; } = []; // Information about the artists involved in the album
    public string? CoverImage { get; init; } // URL to the cover image of the album
    public string? ThumbnailImage { get; init; } // URL to the thumbnail image of the album
    public ReleaseInfo ReleaseInfo { get; init; } = new(); // Information about the album's release
    public bool IsVisible { get; init; } = true; // Indicates if the album is visible to users
}