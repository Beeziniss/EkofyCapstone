using EkofyApp.Application.Mappers;
using EkofyApp.Domain.Entities;


namespace EkofyApp.Application.Models.Track
{
    public class TrackResponse : IMapFrom<Tracks>
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;


    }
}
