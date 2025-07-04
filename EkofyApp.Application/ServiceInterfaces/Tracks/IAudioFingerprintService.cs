using EkofyApp.Application.Models.Wavs;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Tracks;
public interface IAudioFingerprintService
{
    Task<double> CompareWithDatabase(WavFileResponse wavFileResponse);
    Task<AudioFingerprint> GenerateFingerprint(WavFileResponse wavFileResponse);
}
