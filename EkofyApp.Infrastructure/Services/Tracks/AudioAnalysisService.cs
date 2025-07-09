using Audio;
using EkofyApp.Application.Models.Wavs;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Infrastructure.Services.Tracks;
public sealed class AudioAnalysisService(AudioAnalyzer.AudioAnalyzerClient client) : IAudioAnalysisService
{
    private readonly AudioAnalyzer.AudioAnalyzerClient _client = client;

    public async Task<AudioFeature> AnalyzeAudioAsync(WavFileResponse wavFileResponse)
    {
        AnalyzeWavRequest request = new()
        {
            WavFilePath = wavFileResponse.OutputWavPath
        };

        AudioFeaturesReply reply = await _client.AnalyzeWavAsync(request);

        AudioFeature audioFeaturesResponse = new()
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
