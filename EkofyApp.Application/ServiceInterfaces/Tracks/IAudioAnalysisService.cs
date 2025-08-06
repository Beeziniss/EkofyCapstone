using EkofyApp.Application.Models.Wavs;
using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.ServiceInterfaces.Tracks;
public interface IAudioAnalysisService
{
    Task<AudioFeature> AnalyzeAudioAsync(WavFileResponse wavFileResponse);
}
