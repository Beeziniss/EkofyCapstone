using EkofyApp.Application.Mappers;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums.Artist;

namespace EkofyApp.Application.Models.Recordings;
public sealed record class CreateRecordingSplitRequest : IMapFrom<RecordingSplit>
{
    public string UserId { get; init; } = null!;
    public ArtistRole ArtistRole { get; init; }
    public decimal Percentage { get; init; } = default;
}
