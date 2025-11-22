using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Albums;

public sealed record class UpdateAlbumRequest
{
    public string AlbumId { get; init; } = null!; // ID of the album to be updated
    public string? Name { get; init; } // Name of the album, e.g., "My Updated Album"
    public string? Description { get; init; } // Description of the album, e.g., "An updated collection of my favorite songs"
    public AlbumType? Type { get; init; } // Type of the album, e.g., Album, Single, EP, etc.
    public List<ContributingArtist>? ArtistInfos { get; init; } // Information about the artists involved in the album
    public string? CoverImage { get; init; } // URL to the cover image of the album
    public string? ThumbnailImage { get; init; } // URL to the thumbnail image of the album
    public ReleaseInfo? ReleaseInfo { get; init; } // Information about the album's release
    public bool? IsVisible { get; init; } // Indicates if the album is visible to users
}