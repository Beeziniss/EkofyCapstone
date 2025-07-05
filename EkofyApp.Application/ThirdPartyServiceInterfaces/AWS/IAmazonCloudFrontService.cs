

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
public interface IAmazonCloudFrontService
{
    byte[] DecryptionKey(string trackId, string token);
    string GenerateHlsToken(string trackId, int expireMinutes = 5);
    IDictionary<string, string> GenerateSignedCookies(string resourcePath, DateTime expiresUtc);
    Task<string> GetBitratePlaylistAsync(string trackId, string bitrate, string token);
    Task<string> GetMasterPlaylistAsync(string trackId, string token);
}
