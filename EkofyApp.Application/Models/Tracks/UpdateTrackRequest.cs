namespace EkofyApp.Application.Models.Tracks;

public sealed record class UpdateTrackRequest
{
    public string TrackId { get; init; } = null!;
    public string? Description { get; init; }
    public List<string>? CategoryIds { get; init; }
    public List<string>? Tags { get; init; }
    public bool? IsPublic { get; init; }
}
