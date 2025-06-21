namespace EkofyApp.Application.Models.Track
{
    public class CreateTrackRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } = null;
        public string ArtistId { get; set; } = string.Empty;
    }
}
