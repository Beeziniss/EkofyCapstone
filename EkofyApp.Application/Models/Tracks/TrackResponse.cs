using EkofyApp.Application.Mappers;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.Models.Tracks
{
    public class TrackResponse : IMapFrom<Domain.Entities.Tracks>
    {
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
