

using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
public interface IAmazonS3Service
{
    Task DeleteOriginalAudioAsync(string trackId);
    Task DownloadOriginalAudioAsync(string trackId, Func<Stream, Task> processStream, AudioFormat audioFormat = AudioFormat.MP3);
    string GetOriginalAudioSignedUrl(string trackId, AudioFormat audioFormat = AudioFormat.MP3, int expiryMinutes = 15);
    Task RemoveTagAsync(string trackId, List<KeyTag> keyTag);
    Task UploadOriginalAudioAsync(Stream audioStream, string trackId, bool isAutoDelete = true);
    Task UploadFolderAsync(string localFolderPath, string trackId);
}
