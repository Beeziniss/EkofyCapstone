﻿namespace EkofyApp.Domain.Entities;
public sealed class AudioFingerprint
{
    public List<byte[]> CompressedFingerprints { get; set; } = [];
    public List<uint> SequenceNumbers { get; set; } = [];
    public List<float> StartsAt { get; set; } = [];
    public List<byte[]> OriginalPoints { get; set; } = [];
    public double Duration { get; set; } // Duration in seconds
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
