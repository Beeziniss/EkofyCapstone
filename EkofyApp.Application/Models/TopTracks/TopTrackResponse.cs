using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.Models.TopTracks
{
    public sealed record class TopTrackResponse
    {
        public List<TopTrackInfo> TracksInfo { get; set; } = [];
    }
}
