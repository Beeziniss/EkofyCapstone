namespace EkofyApp.Application.Models.Tracks;

public class CreateTrackRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = null;
    public List<string> ArtistId { get; set; } = [];
}
