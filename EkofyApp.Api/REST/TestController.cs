using Amazon.Runtime.Internal.Transform;
using Amazon.S3;
using Amazon.S3.Model;
using AutoMapper;
using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.Models.AudioFeatures;
using EkofyApp.Application.Models.Wavs;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Settings.AWS;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using System.Net;
using System.Text.RegularExpressions;

namespace EkofyApp.Api.REST;
[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    [Authorize(Roles = "Artist"), HttpPost("upload-mp3")]
    public async Task<IActionResult> UploadMp3(IFormFile file, [FromServices] IAmazonS3Service amazonS3Service, [FromServices] IUnitOfWork unitOfWork, [FromServices] IRedisCacheService redisCacheService)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

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
            CategoryIds = [],
            Tags = ["cho phép gắn sẵn"],
            MainArtistIds = [],
        };

        //await unitOfWork.GetCollection<Track>().InsertOneAsync(track);

        // Lưu request vào redis
        await redisCacheService.SetAsync($"track:{trackId}:requestUpload", track, TimeSpan.FromDays(3));

        // Upload file lên S3
        await amazonS3Service.UploadOriginalAudioAsync(stream, trackId);

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

        // Tài nguyên từ S3
        await amazonS3Service.DownloadOriginalAudioAsync(trackId, async stream =>
        {
            string tempName = ObjectId.GenerateNewId().ToString();

            // Convert sang WAV
            AudioConvertPathOptions audioConvertPathOptionsWav = AudioConvertPathOptions.ForConvertToWav();

            // Convert file sang định dạng wav
            wavFileResponse = await ffmpegService.ConvertToWavAsync(stream, tempName, audioConvertPathOptionsWav);
        });

        // Tài nguyên từ file vật lý
        // TODO: Viết hàm xử lý batch HLS folder
        // Purpose: Thêm data thủ công
        // Resolved: Đã có hàm upload multiple files manually

        // 1. Tạo hls từ file wav
        AudioConvertPathOptions audioConvertPathOptionsHls = AudioConvertPathOptions.ForConvertToHls(trackId);
        string outputHlsPath = await ffmpegService.ConvertToHlsAsync(wavFileResponse, audioConvertPathOptionsHls);

        // 2. Tạo fingerprint từ file wav
        AudioFingerprint audioFingerprint = await fingerprintCustomService.GenerateFingerprint(wavFileResponse);

        // 3. Lấy đặc trưng âm thanh từ python service
        AudioFeature audioAnalysisResponse = await audioAnalysisService.AnalyzeAudioAsync(wavFileResponse);

        // Xác định mood của track dựa trên đặc trưng âm thanh
        IEnumerable<MoodType> moodTypes = HelperMethod.DetectMoods(audioAnalysisResponse);
        IEnumerable<string> moodIds = [];

        if (moodTypes.Any())
        {
            moodIds = await unitOfWork.GetCollection<Category>()
                .Find(mood => mood.Type == CategoryType.Mood && moodTypes.Contains(Enum.Parse<MoodType>(mood.Name)))
                .Project(mood => mood.Id)
                .ToListAsync();
        }

        // Phase 3: Lưu trữ
        // Ở phase này sẽ tổng hợp lại tất cả các kết quả phân tích
        // Sau đó lưu trữ vào cơ sở dữ liệu

        // Cập nhật track với các thông tin đã phân tích
        UpdateDefinition<Track> updateDefinition = Builders<Track>.Update
            .Set(track => track.CategoryIds, moodIds)
            .Set(track => track.AudioFingerprint, audioFingerprint)
            .Set(track => track.AudioFeature, audioAnalysisResponse)
            .Set(track => track.UpdatedAt, HelperMethod.GetUtcPlus7Time());

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

        var result = await fingerprintCustomService.GetMatchConfidenceScore(wavFileResponse);

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

    // Upload UseCase Handler
    [HttpPost("upload-multiple")]
    public async Task<IActionResult> UploadMultipleFilesManually(string trackId, [FromServices] IFfmpegService ffmpegService, [FromServices] IAmazonS3Service amazonS3Service, [FromServices] IAudioFingerprintService fingerprintCustomService, [FromServices] IAudioAnalysisService audioAnalysisService, [FromServices] IUnitOfWork unitOfWork)
    {

        // Tài nguyên từ file vật lý
        // TODO: Viết hàm xử lý batch HLS folder
        // Purpose: Thêm data thủ công
        string inputRootFolder = "Z:\\Projects\\EkofyProject\\Tracks\\Arcane";
        string inputMP3Folder = System.IO.Path.Combine(inputRootFolder, "MP3");
        string outputWavFolder = System.IO.Path.Combine(inputRootFolder, "WAV", "Temp");
        string outputHLSFolder = System.IO.Path.Combine(inputRootFolder, "HLS");

        string[] mp3Files = Directory.GetFiles(inputMP3Folder, "*.mp3");

        if (!Directory.Exists(outputWavFolder))
        {
            Directory.CreateDirectory(outputWavFolder);
        }

        int count = 0;
        foreach (string mp3File in mp3Files)
        {
            // Convert sang WAV
            AudioConvertPathOptions audioConvertPathOptionsWav = AudioConvertPathOptions.CreateCustom(inputRootFolder, null, outputWavFolder);

            // Convert file sang định dạng wav
            WavFileResponse wavFileResponse = await ffmpegService.ConvertToWavAsync(mp3File, audioConvertPathOptionsWav);
            // Lưu tạm file wav để xử lý tiếp
            if (wavFileResponse == null || string.IsNullOrEmpty(wavFileResponse.OutputWavPath))
            {
                return BadRequest("Failed to convert MP3 to WAV.");
            }

            // 1. Tạo hls từ file wav
            AudioConvertPathOptions audioConvertPathOptionsHls = AudioConvertPathOptions.CreateCustom(inputRootFolder, null, outputHLSFolder);
            string outputHlsPath = await ffmpegService.ConvertToHlsAsync(wavFileResponse, audioConvertPathOptionsHls);

            count++;
        }

        HelperMethod.DeleteBatchIO(outputWavFolder);

        //// 2. Tạo fingerprint từ file wav
        //AudioFingerprint audioFingerprint = await fingerprintCustomService.GenerateFingerprint(wavFileResponse);

        //// 3. Lấy đặc trưng âm thanh từ python service
        //AudioFeature audioAnalysisResponse = await audioAnalysisService.AnalyzeAudioAsync(wavFileResponse);

        //// Xác định mood của track dựa trên đặc trưng âm thanh
        //IEnumerable<MoodType> moodTypes = HelperMethod.DetectMoods(audioAnalysisResponse);
        //IEnumerable<string> moodIds = [];

        //if (moodTypes.Any())
        //{
        //    moodIds = await unitOfWork.GetCollection<Category>()
        //        .Find(mood => mood.Type == CategoryType.Mood && moodTypes.Contains(Enum.Parse<MoodType>(mood.Name)))
        //        .Project(mood => mood.Id)
        //        .ToListAsync();
        //}

        //// Phase 3: Lưu trữ
        //// Ở phase này sẽ tổng hợp lại tất cả các kết quả phân tích
        //// Sau đó lưu trữ vào cơ sở dữ liệu

        //// Cập nhật track với các thông tin đã phân tích
        //UpdateDefinition<Track> updateDefinition = Builders<Track>.Update
        //    .Set(track => track.CategoryIds, moodIds)
        //    .Set(track => track.AudioFingerprint, audioFingerprint)
        //    .Set(track => track.AudioFeature, audioAnalysisResponse)
        //    .Set(track => track.UpdatedAt, HelperMethod.GetUtcPlus7Time());

        //await unitOfWork.GetCollection<Track>().FindOneAndUpdateAsync(track => track.Id == trackId, updateDefinition);

        //// Đẩy hls playlist lên S3
        //await amazonS3Service.UploadFolderAsync(outputHlsPath, trackId);

        //// Xóa folder, file tạm sau khi upload lên S3
        //if (Directory.Exists(outputHlsPath))
        //{
        //    Directory.Delete(outputHlsPath, true);
        //}
        //if (System.IO.File.Exists(wavFileResponse.OutputWavPath))
        //{
        //    System.IO.File.Delete(wavFileResponse.OutputWavPath);
        //}

        return Ok(new
        {
            Message = "Upload UseCase Handler Successfully",
            Count = count,
            FilesCount = mp3Files.Length
        });
    }

    [HttpGet("original-audio/{trackId}")]
    public IActionResult GetOriginalAudio([FromServices] IAmazonCloudFrontService amazonS3Service, string trackId)
    {
        if (string.IsNullOrEmpty(trackId))
        {
            return BadRequest("Track ID is required.");
        }

        // Byte-range request handling
        return Ok(new
        {
            Message = "Original audio retrieved successfully",
            Url = amazonS3Service.GenerateOriginalSignedURL(trackId)
        });
    }

    [HttpGet("generate")]
    public IActionResult GeneratePdf()
    {
        // Tạo tài liệu PDF mới
        using (PdfDocument document = new PdfDocument())
        {
            PdfPage page = document.Pages.Add();
            PdfGraphics graphics = page.Graphics;

            PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 20);
            graphics.DrawString("Hello from Syncfusion PDF!", font, PdfBrushes.Black, new Syncfusion.Drawing.PointF(0, 0));

            using (MemoryStream ms = new MemoryStream())
            {
                document.Save(ms);
                ms.Position = 0;
                return File(ms.ToArray(), "application/pdf", "hello-syncfusion.pdf");
            }
        }
    }

    [HttpPost("subscription-plan")]
    public IActionResult CreateSubscriptionPlan([FromServices] IStripeService stripeService, string lookupKey, long amount, string subscriptionPlanName)
    {
        var plan = stripeService.CreateSubscriptionPlan(lookupKey, subscriptionPlanName, amount);
        return Ok(new
        {
            Message = "Create Subscription Plan Successfully",
            plan
        });
    }

    [HttpDelete("delete-account-stripe")]
    public async Task<IActionResult> DeleteAccountStripe(string accountId, [FromServices] IStripeService stripeService)
    {
        await stripeService.DeleteConnectedAccount(accountId);
        return Ok(new
        {
            Message = "Delete Stripes Account Successfully",
        });
    }

    //[HttpGet("replace-content-optimized-1")]
    //public IActionResult ReplaceContentOptimized1([FromServices] IFfmpegService ffmpegService)
    //{
    //    string result = ffmpegService.Testing();

    //    return Ok(result);
    //}

    //[HttpGet("replace-content-optimized-2")]
    //public IActionResult ReplaceContentOptimized2([FromServices] IFfmpegService ffmpegService)
    //{
    //    string result = ffmpegService.Testing2();
    //    return Ok(result);
    //}
}
