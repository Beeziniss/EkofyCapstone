using Audio;
using EkofyApp.Application.Models.AudioFeatures;
using EkofyApp.Application.ServiceInterfaces.Tracks;

namespace EkofyApp.Infrastructure.Services.Tracks;
public class AudioAnalysisService(AudioAnalyzer.AudioAnalyzerClient client) : IAudioAnalysisService
{
    private readonly AudioAnalyzer.AudioAnalyzerClient _client = client;

    public async Task<AudioFeaturesResponse> AnalyzeAudioAsync(Stream wavStream)
    {
        await using MemoryStream ms = new();
        await wavStream.CopyToAsync(ms);
        ms.Position = 0;

        AnalyzeWavRequest request = new()
        {
            WavData = Google.Protobuf.ByteString.CopyFrom(ms.ToArray())
        };

        AudioFeaturesReply reply = await _client.AnalyzeWavAsync(request);

        AudioFeaturesResponse audioFeaturesResponse = new()
        {
            Tempo = reply.Tempo,
            Key = reply.Key,
            KeyNumber = reply.KeyNumber,
            Mode = reply.Mode,
            ModeNumber = reply.ModeNumber,
            Energy = reply.Energy,
            Danceability = reply.Danceability,
            Acousticness = reply.Acousticness,
            SpectralCentroid = reply.SpectralCentroid,
            ZeroCrossingRate = reply.ZeroCrossingRate,
            Duration = reply.Duration,
            ChromaMean = reply.ChromaMean.ToList(),
            MfccMean = reply.MfccMean.ToList()
        };

        return audioFeaturesResponse;
    }
}
