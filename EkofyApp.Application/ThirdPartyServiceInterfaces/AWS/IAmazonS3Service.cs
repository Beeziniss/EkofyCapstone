

using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
public interface IAmazonS3Service
{
    Task DownloadOriginalAudioAsync(string trackId, Func<Stream, Task> processStream, AudioFormat audioFormat = AudioFormat.MP3);
    string GetOriginalAudioSignedUrl(string trackId, AudioFormat audioFormat = AudioFormat.MP3, int expiryMinutes = 15);
    Task UploadFileAsync(Stream audioStream, string trackId);
    Task UploadFolderAsync(string localFolderPath, string trackId);
}
