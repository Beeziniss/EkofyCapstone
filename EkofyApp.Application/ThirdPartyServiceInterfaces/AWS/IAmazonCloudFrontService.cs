

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
public interface IAmazonCloudFrontService
{
    byte[] DecryptionKey(string trackId, string token);
    string GenerateHlsToken(string trackId, int expireMinutes = 5);
    string GenerateSignedRedirect(string trackId, string bitrate, string segment, string token);
    Task<string> GetBitratePlaylistAsync(string trackId, string bitrate, string token);
    Task<string> GetMasterPlaylistAsync(string trackId, string token);
    string RefreshSignedUrl(string trackId, string oldToken, int expireMinutes = 5);
}
