using EkofyApp.Domain.Base;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class AudioFingerprint : TimeStamped
{
    public List<byte[]> CompressedFingerprints { get; set; } = [];
    public List<uint> SequenceNumbers { get; set; } = [];
    public List<float> StartsAt { get; set; } = [];
    public List<byte[]> OriginalPoints { get; set; } = [];
    public double Duration { get; set; } // Duration in seconds
}
