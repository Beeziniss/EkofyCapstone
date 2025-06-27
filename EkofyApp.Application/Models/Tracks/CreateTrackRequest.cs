namespace EkofyApp.Application.Models.Tracks;

public class CreateTrackRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = null;
    public string ArtistId { get; set; } = string.Empty;
}
