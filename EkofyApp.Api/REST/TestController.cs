using EkofyApp.Application.Models.AudioFeatures;
using EkofyApp.Application.Models.Wavs;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HealthyNutritionApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EkofyApp.Api.REST;
[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    //[HttpPost("analyze-grpc")]
    //public async Task<IActionResult> AnalyzeAudioGrpc(IFormFile file, [FromServices] IAudioAnalysisService audioAnalysisService)
    //{
    //    if (file == null || file.Length == 0)
    //        return BadRequest("No file uploaded.");

    //    using var stream = file.OpenReadStream();

    //    var reply = await audioAnalysisService.AnalyzeAudioAsync(stream);

    //    return Ok(reply);
    //}

    [HttpPost("upload-mp3")]
    public async Task<IActionResult> UploadMp3(IFormFile file, [FromServices] IAmazonS3Service amazonS3Service, [FromServices] IUnitOfWork unitOfWork)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        using Stream stream = file.OpenReadStream();
        string fileName = System.IO.Path.GetFileNameWithoutExtension(file.FileName);
        string trackId = ObjectId.GenerateNewId().ToString();

        string categoryId = ObjectId.GenerateNewId().ToString();

        // Lưu thông tin track vào cơ sở dữ liệu
        Track track = new()
        {
            Id = trackId,
            Name = "Name",
            Description = "Uploaded MP3 file",
            CategoryIds = [$"{categoryId}"],
            Tags = ["Tag1", "Tag2"],
            ArtistId = ObjectId.GenerateNewId().ToString(), // ObjectId (string) của Artist
            CreatedAt = TimeControl.GetUtcPlus7Time(),
        };

        await unitOfWork.GetCollection<Track>().InsertOneAsync(track);

        // Upload file lên S3
        await amazonS3Service.UploadFileAsync(stream, trackId);

        return Ok(new
        {
            Message = "Upload MP3 Successfully",
        });
    }

    // Upload UseCase Handler
    [HttpPost("upload")]
    public async Task<IActionResult> HandleUploadUsecase(string trackId, [FromServices] IFfmpegService ffmpegService, [FromServices] IAmazonS3Service amazonS3Service, [FromServices] IAudioFingerprintService fingerprintCustomService, [FromServices] IAudioAnalysisService audioAnalysisService, [FromServices] IUnitOfWork unitOfWork)
    {
        // Phase 1: Kiểm duyệt
        // Khởi tạo track entity
        // Lưu file gốc vào S3 để kiểm duyệt
        // Kiểm tra tự động -> không cần moderator duyệt
        // Kiểm tra thủ công -> cần moderator duyệt
        // Kiểm tra tự động: Audio file có định dạng hợp lệ, vi phạm chính sách không (bao gồm cả vi phạm bản quyền)
        // Nếu có vi phạm cần moderator kiểm tra thủ công
        // Nếu không có vi phạm thì chuyển sang Phase 2


        // Phase 2: Phân tích
        // Convert file sang định dạng Wav
        // Chia thêm 2 phase nhỏ: convert wav sang hls và tạo fingerprint, trích xuất đặc trưng âm thanh

        WavFileResponse wavFileResponse = default!;

        await amazonS3Service.DownloadOriginalAudioAsync(trackId, async stream =>
        {
            string tempName = ObjectId.GenerateNewId().ToString();
            // Convert sang WAV
            AudioConvertPathOptions audioConvertPathOptionsWav = AudioConvertPathOptions.ForConvertToWav();

            // Convert file sang định dạng wav
            wavFileResponse = await ffmpegService.ConvertToWavAsync(stream, tempName, audioConvertPathOptionsWav);
        });

        // 1. Tạo hls từ file wav
        AudioConvertPathOptions audioConvertPathOptionsHls = AudioConvertPathOptions.ForConvertToHls(trackId);
        string outputHlsPath = await ffmpegService.ConvertToHlsAsync(wavFileResponse, audioConvertPathOptionsHls);

        // 2. Tạo fingerprint từ file wav
        AudioFingerprint audioFingerprint = await fingerprintCustomService.GenerateFingerprint(wavFileResponse);

        // 3. Lấy đặc trưng âm thanh từ python service
        AudioFeature audioAnalysisResponse = await audioAnalysisService.AnalyzeAudioAsync(wavFileResponse);

        // Phase 3: Lưu trữ
        // Ở phase này sẽ tổng hợp lại tất cả các kết quả phân tích
        // Sau đó lưu trữ vào cơ sở dữ liệu

        UpdateDefinition<Track> updateDefinition = Builders<Track>.Update
            .Set(track => track.AudioFingerprint, audioFingerprint)
            .Set(track => track.AudioFeature, audioAnalysisResponse)
            .Set(track => track.UpdatedAt, TimeControl.GetUtcPlus7Time());

        await unitOfWork.GetCollection<Track>().FindOneAndUpdateAsync(track => track.Id == trackId, updateDefinition);

        // Đẩy hls playlist lên S3
        await amazonS3Service.UploadFolderAsync(outputHlsPath, trackId);

        // Xóa folder, file tạm sau khi upload lên S3
        if (Directory.Exists(outputHlsPath))
        {
            Directory.Delete(outputHlsPath, true);
        }
        if (System.IO.File.Exists(wavFileResponse.OutputWavPath))
        {
            System.IO.File.Delete(wavFileResponse.OutputWavPath);
        }

        return Ok(new
        {
            Message = "Upload UseCase Handler Successfully",
        });
    }

    [HttpPost("recognization")]
    public async Task<IActionResult> RecognizeAudio(IFormFile file, [FromServices] IAudioFingerprintService fingerprintCustomService, [FromServices] IFfmpegService ffmpegService)
    {
        using Stream stream = file.OpenReadStream();

        AudioConvertPathOptions audioConvertPathOptions = AudioConvertPathOptions.ForConvertToWav();

        WavFileResponse wavFileResponse = await ffmpegService.ConvertToWavAsync(stream, "aaaaaaa", audioConvertPathOptions);

        var result = await fingerprintCustomService.CompareWithDatabase(wavFileResponse);

        // Xóa file tạm sau khi nhận diện xong
        if (System.IO.File.Exists(wavFileResponse.OutputWavPath))
        {
            System.IO.File.Delete(wavFileResponse.OutputWavPath);
        }

        return Ok(new
        {
            Message = "Audio recognition completed",
            result
        });
    }
}
