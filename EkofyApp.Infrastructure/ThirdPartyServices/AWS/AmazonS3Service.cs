using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Settings.AWS;
using System.Net;

namespace EkofyApp.Infrastructure.ThirdPartyServices.AWS;
public class AmazonS3Service(IAmazonS3 s3Client, AWSSetting aWSSettings) : IAmazonS3Service
{
    private readonly IAmazonS3 _s3Client = s3Client;
    private readonly AWSSetting _aWSSettings = aWSSettings;

    public string GetOriginalAudioSignedUrl(string trackId, AudioFormat audioFormat = AudioFormat.MP3, int expiryMinutes = 15)
    {
        string s3Key = $"original-audio/{trackId}";

        GetPreSignedUrlRequest signedUrlRequest = new()
        {
            BucketName = _aWSSettings.BucketName,
            Key = s3Key,
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Verb = HttpVerb.GET
        };

        return _s3Client.GetPreSignedURL(signedUrlRequest);
    }

    public async Task DownloadOriginalAudioAsync(string trackId, Func<Stream, Task> processStream, AudioFormat audioFormat = AudioFormat.MP3)
    {
        string bucketName = _aWSSettings.BucketName;
        string s3Key = $"original-audio/{trackId}";

        GetObjectRequest request = new()
        {
            BucketName = bucketName,
            Key = s3Key
        };

        using GetObjectResponse response = await _s3Client.GetObjectAsync(request);
        await processStream(response.ResponseStream); // Tự Dispose sau khi callback xong
    }

    public async Task UploadFileAsync(Stream audioStream, string trackId)
    {
        string bucket = _aWSSettings.BucketName;
        string key = $"original-audio/{trackId}";

        var putRequest = new PutObjectRequest
        {
            InputStream = audioStream,
            BucketName = bucket,
            Key = key,
            ContentType = "audio/mpeg"
        };

        // Đặt điều kiện: chỉ upload nếu object không tồn tại
        putRequest.Headers["If-None-Match"] = "*";

        try
        {
            await _s3Client.PutObjectAsync(putRequest);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            // Object đã tồn tại
            throw new BadRequestCustomException("File already exists in S3");
        }
    }

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

            string outputFolder = "streaming-audio";
            string s3Key = $"{outputFolder}/{trackId}"; // Định nghĩa đường dẫn folder trên S3

            await transferUtility.UploadDirectoryAsync(new TransferUtilityUploadDirectoryRequest
            {
                BucketName = _aWSSettings.BucketName,
                Directory = localFolderPath,
                KeyPrefix = s3Key,
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
