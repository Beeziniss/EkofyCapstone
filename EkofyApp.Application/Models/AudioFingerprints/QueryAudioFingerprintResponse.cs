namespace EkofyApp.Application.Models.AudioFingerprints;
public sealed record class QueryAudioFingerprintResponse
{
    public string TrackId { get; init; } = null!;
    public string TrackName { get; init; } = null!;
    public string ArtistId { get; init; } = null!;
    public string ArtistName { get; init; } = null!;

    public string MediaType { get; init; } = null!; // Media type (e.g., "audio", "video")

    public double QueryMatchStartsAt { get; init; } // Start time of the match in the query audio (in seconds)
    public double QueryMatchEndsAt { get; init; } // End time of the match in the query audio (in seconds)
    public double TrackMatchStartsAt { get; init; } // Start time of the match in the reference track (in seconds)
    public double TrackMatchEndsAt { get; init; } // End time of the match in the reference track (in seconds)

    public double QueryCoverageLength { get; init; } // Length of the matched segment in the query audio (in seconds)
    public double TrackCoverageLength { get; init; } // Length of the matched segment in the reference track (in seconds)

    public double QueryCoverage { get; init; } // QueryCoverage score (0 to 100)
    public double TrackCoverage { get; init; } // TrackCoverage score (0 to 100)

    public double MinConfidence { get; init; } = 0; // MinConfidence score (0 to 100)
    public double MinCoverage { get; init; } = 0; // MinCoverage score (0 to 100)
}
