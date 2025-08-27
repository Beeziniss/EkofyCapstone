using EkofyApp.Application.Mappers;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums.Artist;

namespace EkofyApp.Application.Models.Works;
public sealed record class CreateWorkSplitRequest : IMapFrom<WorkSplit>
{
    public string UserId { get; init; } = null!;
    public ArtistRole ArtistRole { get; init; }
    public decimal Percentage { get; init; } = default;
}