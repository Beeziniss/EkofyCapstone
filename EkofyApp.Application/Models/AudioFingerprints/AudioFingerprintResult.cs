namespace EkofyApp.Application.Models.AudioFingerprints;
public sealed record class AudioFingerprintResult
{
    public string TrackId { get; init; } = null!;
    public string TrackName { get; init; } = null!;
    public string ArtistId { get; init; } = null!;

    public double BestConfidence { get; init; } = 0; // BestConfidence score (0 to 100)
}
