
namespace EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
public interface IAmazonS3Service
{
    Task UploadFolderAsync(string localFolderPath, string trackId);
}
