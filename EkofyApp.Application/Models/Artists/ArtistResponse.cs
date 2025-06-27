using EkofyApp.Application.Mappers;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.Models.Artists;
public class ArtistResponse : IMapFrom<Artist>
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Genre { get; set; } = default!;
}
