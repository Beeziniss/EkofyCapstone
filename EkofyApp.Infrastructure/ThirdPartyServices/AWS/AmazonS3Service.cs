using Amazon.S3;
using Amazon.S3.Transfer;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Settings.AWS;

namespace EkofyApp.Infrastructure.ThirdPartyServices.AWS;
public class AmazonS3Service(IAmazonS3 s3Client, AWSSetting aWSSettings) : IAmazonS3Service
{
    private readonly IAmazonS3 _s3Client = s3Client;
    private readonly AWSSetting _aWSSettings = aWSSettings;

    // Hàm này upload file audio stream lên S3 nhưng mà hài lắm
    //public async Task<string> UploadFileAsync(Stream audioStream, string fileName, string trackIdName)
    //{
    //    if (audioStream == null || audioStream.Length == 0)
    //    {
    //        throw new BadRequestCustomException("AudioStream is empty");
    //    }

    //    // Lấy thông tin từ settings
    //    string bucketName = _aWSSettings.BucketName;
    //    string region = _aWSSettings.Region;

    //    if (string.IsNullOrEmpty(bucketName))
    //    {
    //        throw new Exception("Missing AWS Configuration in Environment Variables");
    //    }

    //    // Chuẩn bị thông tin file
    //    string outputFolder = "streaming-audio/";
    //    string fullFileName = trackIdName + Path.GetExtension(fileName);
    //    string s3Key = outputFolder + fullFileName;

    //    // Kiểm tra xem file đã tồn tại trên S3 chưa (Tránh upload lại)
    //    try
    //    {
    //        await _s3Client.GetObjectMetadataAsync(bucketName, s3Key);
    //        Console.WriteLine($"AudioFile {s3Key} đã tồn tại trên S3, không cần upload lại.");
    //    }
    //    catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    //    {
    //        // AudioFile chưa tồn tại → Thực hiện upload
    //        audioStream.Position = 0; // Đảm bảo stream ở đầu
    //        using Stream uploadStream = audioStream; // Dispose stream sau khi upload

    //        TransferUtilityUploadRequest uploadRequest = new()
    //        {
    //            InputStream = uploadStream,
    //            BucketName = bucketName,
    //            Key = s3Key,
    //            ContentType = "audio/mpeg"
    //        };

    //        using var fileTransferUtility = new TransferUtility(_s3Client);
    //        await fileTransferUtility.UploadAsync(uploadRequest);
    //    }

    //    // URL public cho file gốc
    //    string s3UrlPublic = $"https://{bucketName}.s3.{region}.amazonaws.com/{s3Key}";

    //    return s3UrlPublic;
    //}

    public async Task UploadFolderAsync(string localFolderPath, string trackId)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(localFolderPath);

            if (!Directory.Exists(localFolderPath))
            {
                throw new DirectoryNotFoundException($"The folder does not exist: {localFolderPath}");
            }

            // Sử dụng TransferUtility để upload cả folder
            TransferUtility transferUtility = new(_s3Client);

            await transferUtility.UploadDirectoryAsync(new TransferUtilityUploadDirectoryRequest
            {
                BucketName = _aWSSettings.BucketName,
                Directory = localFolderPath,
                KeyPrefix = $"streaming-audio/{trackId}", // Định dạng folder trên S3
                SearchOption = SearchOption.AllDirectories, // Upload cả file trong sub-folder nếu có
                                                            //CannedACL = S3CannedACL.PublicRead // (Tùy chọn) Set quyền public nếu cần
            });
        }
        catch (AmazonS3Exception ex)
        {
            //Console.WriteLine($"Error encountered on server. Message:'{ex.Message}' when uploading folder {localFolderPath} to S3.");
            throw new ExternalServiceCustomException($"Error uploading folder to S3: {ex.Message}");
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"Unknown encountered on server. Message:'{ex.Message}' when uploading folder {localFolderPath} to S3.");
            throw new Exception($"Unknown error uploading folder to S3: {ex.Message}");
        }
        finally
        {
            // Xóa thư mục output/trackId sau khi upload
            if (Directory.Exists(localFolderPath))
            {
                Directory.Delete(localFolderPath, true);
            }
        }

        return;
    }
}
