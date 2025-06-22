using EkofyApp.Application.Mappers;
using EkofyApp.Application.Models.Artist;
using EkofyApp.Domain.Entities;


namespace EkofyApp.Application.Models.Track
{
    public class TrackResponse : IMapFrom<Tracks>
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;

        public string ArtistId { get; set; } = default!;

        public ArtistResponse Artist { get; set; } = default!;
    }
}
