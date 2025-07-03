
using EkofyApp.Application.Models.AudioFeatures;

namespace EkofyApp.Application.ServiceInterfaces.Tracks;
public interface IAudioAnalysisService
{
    Task<AudioFeaturesResponse> AnalyzeAudioAsync(Stream wavStream);
}
