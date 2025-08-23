using EkofyApp.Application.Models.AudioFingerprints;
using EkofyApp.Application.Models.Wavs;
using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.ServiceInterfaces.Tracks;
public interface IAudioFingerprintService
{
    Task<AudioFingerprintResult> GetMatchConfidenceScore(WavFileResponse wavFileResponse);
    Task<AudioFingerprint> GenerateFingerprint(WavFileResponse wavFileResponse);
}
