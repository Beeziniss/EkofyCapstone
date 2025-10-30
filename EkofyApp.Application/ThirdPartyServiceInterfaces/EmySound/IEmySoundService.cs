using EkofyApp.Application.Models.AudioFingerprints;
using Refit;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.EmySound;
public interface IEmySoundService
{
    Task<IEnumerable<QueryAudioFingerprintResponse>> CheckTrackFingerprintAsync(byte[] fileBytes, string fileName, string contentType);
    Task<string> UploadTrackFingerprintAsync(Stream stream, string trackId, string trackName, string artistName, string artistId);
}
