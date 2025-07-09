using EkofyApp.Application.Mappers;
using EkofyApp.Application.Models.Artists;
using EkofyApp.Domain.Entities;


namespace EkofyApp.Application.Models.Tracks;

public record TrackResponse : IMapFrom<Track>
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;

    public string ArtistId { get; set; } = default!;

    public ArtistResponse Artist { get; set; } = default!;
}
