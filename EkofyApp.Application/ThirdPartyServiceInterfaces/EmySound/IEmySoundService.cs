using EkofyApp.Application.Models.AudioFingerprints;
using Refit;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.EmySound;
public interface IEmySoundService
{
    Task<IEnumerable<QueryAudioFingerprintResponse>> CheckTrackFingerprintAsync(StreamPart streamPart);
    Task<string> UploadTrackFingerprintAsync(Stream stream, string trackId, string trackName, string artistId);
}
