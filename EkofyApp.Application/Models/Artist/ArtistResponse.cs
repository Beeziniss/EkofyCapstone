using EkofyApp.Application.Mappers;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.Models.Artist;
public class ArtistResponse : IMapFrom<Artists>
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Genre { get; set; } = default!;
}
