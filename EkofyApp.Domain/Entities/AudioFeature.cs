namespace EkofyApp.Domain.Entities;
public sealed class AudioFeature
{
    public float Tempo { get; set; }
    public string Key { get; set; }
    public int KeyNumber { get; set; }
    public string Mode { get; set; }
    public int ModeNumber { get; set; }
    public float Energy { get; set; }
    public float Danceability { get; set; }
    public float Acousticness { get; set; }
    public float SpectralCentroid { get; set; }
    public float ZeroCrossingRate { get; set; }
    public float Duration { get; set; }

    public List<float> ChromaMean { get; set; }
    public List<float> MfccMean { get; set; }
}
