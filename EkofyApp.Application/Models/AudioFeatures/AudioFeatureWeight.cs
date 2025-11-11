namespace EkofyApp.Application.Models.AudioFeatures;

public sealed record class AudioFeatureWeight
{
    public float? Tempo { get; init; }
    public float? Energy { get; init; }
    public float? Danceability { get; init; }
    public float? Acousticness { get; init; }
}