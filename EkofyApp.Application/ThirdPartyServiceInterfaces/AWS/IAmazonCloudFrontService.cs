

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
public interface IAmazonCloudFrontService
{
    void ValidateHlsToken(string expectedTrackId, string token);
    byte[] DecryptionKey(string trackId, string token);
    string GenerateHlsToken(string trackId, int expireMinutes = 5);
    string RefreshSignedUrl(string trackId, string oldToken, int expireMinutes = 5);
    Task<string> GetMasterPlaylistAsync(string trackId, string token);
    Task<string> GetBitratePlaylistAsync(string trackId, string bitrate, string token);
    string GenerateSignedRedirect(string trackId, string bitrate, string segment, string token);
}
