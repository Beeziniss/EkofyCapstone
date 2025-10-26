namespace EkofyApp.Application.Models.AudioFingerprints;
public sealed record AudioFingerprintResponse
{
    public List<byte[]> CompressedFingerprints { get; init; } = [];
    public List<uint> SequenceNumbers { get; init; } = [];
    public List<float> StartsAt { get; init; } = [];
    public List<byte[]> OriginalPoints { get; init; } = [];
    public double Duration { get; init; } // Thời lượng tính bằng giây
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
