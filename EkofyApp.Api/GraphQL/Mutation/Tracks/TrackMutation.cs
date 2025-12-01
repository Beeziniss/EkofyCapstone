using EkofyApp.Application.Models.ApprovalHistories;
using EkofyApp.Application.Models.AudioFingerprints;
using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Uploads;
using EkofyApp.Application.Models.Wavs;
using EkofyApp.Application.Models.Works;
using EkofyApp.Application.ServiceInterfaces.ApprovalHistories;
using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Application.ServiceInterfaces.Categories;
using EkofyApp.Application.ServiceInterfaces.Jobs;
using EkofyApp.Application.ServiceInterfaces.Recordings;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Application.ServiceInterfaces.Works;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Application.ThirdPartyServiceInterfaces.EmySound;
using EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Hangfire;
using HotChocolate.Subscriptions;
using MongoDB.Bson;
using Refit;

namespace EkofyApp.Api.GraphQL.Mutation.Tracks;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class TrackMutation(ITrackService trackService, IArtistService artistService, IRedisCacheService redisCacheService, IAmazonS3Service amazonS3Service, IFfmpegService ffmpegService, IAudioAnalysisService audioAnalysisService, ICategoryService categoryService, IWorkService workService, IRecordingService recordingService, IEmySoundService emySoundService, IApprovalHistoryService approvalHistoryService, IUserService userService, IHttpContextAccessor httpContextAccessor)
{
    private readonly ITrackService _trackService = trackService;
    private readonly IArtistService _artistService = artistService;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;
    private readonly IAmazonS3Service _amazonS3Service = amazonS3Service;
    private readonly IFfmpegService _ffmpegService = ffmpegService;
    private readonly IAudioAnalysisService _audioAnalysisService = audioAnalysisService;
    private readonly ICategoryService _categoryService = categoryService;
    private readonly IWorkService _workService = workService;
    private readonly IRecordingService _recordingService = recordingService;
    private readonly IEmySoundService _emySoundService = emySoundService;
    private readonly IApprovalHistoryService _approvalHistoryService = approvalHistoryService;
    private readonly IUserService _userService = userService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<bool> UploadTrackAsync(IFile file, CreateTrackRequest createTrackRequest, CreateWorkRequest createWorkRequest, CreateRecordingRequest createRecordingRequest, bool isTesting = false)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
        string artistId = _httpContextAccessor.HttpContext?.User.FindFirst("artistId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
        createTrackRequest = createTrackRequest with { CreatedByArtistId = artistId };
        createTrackRequest = createTrackRequest with { CreatedByUserId = userId };

        // Kiểm tra hạn chế upload track
        bool hasAnyRestriction = await _userService.CheckMultipleRestrictionsAsync(RestrictionAction.UploadTrack);
        if (hasAnyRestriction)
        {
            throw new UnauthorizedCustomException("You are restricted from uploading track.");
        }

        //using Stream stream = file.OpenReadStream();
        // Đọc toàn bộ file vào mảng byte[]
        byte[] fileBytes;
        using (Stream tempStream = file.OpenReadStream())
        {
            using MemoryStream memoryStream = new();
            await tempStream.CopyToAsync(memoryStream);
            fileBytes = memoryStream.ToArray();
        }

        // Sử dụng memory stream cho fingerprint
        using MemoryStream fingerprintStream = new(fileBytes);
        StreamPart streamPart = new(fingerprintStream, file.Name, file.ContentType);

        if (createTrackRequest.IsOriginal)
        {
            //Kiểm tra bản quyền
            // Reset lại vị trí của stream về đầu
            fingerprintStream.Position = 0;

            IEnumerable<QueryAudioFingerprintResponse> responses = await _emySoundService.CheckTrackFingerprintAsync(fileBytes, file.Name, file.ContentType ?? throw new ConflictCustomException("Content type file is empty or null"));
            if (responses.Any())
            {
                QueryAudioFingerprintResponse bestMatch = responses.OrderByDescending(r => r.QueryCoverage).First();

                switch (bestMatch.MinConfidence)
                {
                    case 0.8:
                        {
                            throw new BadRequestCustomException($"The uploaded track is likely to infringe copyright.\nScore: {bestMatch.MinConfidence}.\nCoverage: {bestMatch.QueryCoverage}.\nTrack: {bestMatch.TrackId} | {bestMatch.TrackName}.");
                        }
                    case 0.7:
                        {
                            // Đánh cờ duyệt thủ công
                            // TODO: Kiểm tra các thông tin metadata cơ bản tự động
                            // Như định dạng file, bitrate, sample rate, duration, v.v.
                            // Lyrics có explicit không: nếu có thì track phải đánh dấu explicit
                            // Còn nếu không có thì track không được đánh dấu explicit
                            // Trường hợp không đánh dấu explicit mà lyrics có từ ngữ nhạy cảm thì sẽ tự động set explicit là true

                            // Tạo stream mới để dùng cho duyệt thủ công
                            using MemoryStream manualStream = new(fileBytes);
                            await AssignApproveManuallyAsync(manualStream, createTrackRequest, createWorkRequest, createRecordingRequest);

                            return true;
                        }
                }
            }

            // Tạo stream mới để dùng cho duyệt tự động
            using var autoStream = new MemoryStream(fileBytes);

            // Duyệt tự động
            if (!isTesting)
            {
                //await ApproveAutomaticallyAsync(autoStream, createTrackRequest, createWorkRequest, createRecordingRequest);

                // Tạo file tạm để truyền vào job
                string uploadsTempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MyAppUploads");
                Directory.CreateDirectory(uploadsTempDir); // đảm bảo tồn tại

                string tempFilePath = System.IO.Path.Combine(uploadsTempDir, ObjectId.GenerateNewId() + System.IO.Path.GetExtension(file.Name));
                await File.WriteAllBytesAsync(tempFilePath, fileBytes);

                // Kiểm tra file đã tạo chưa
                if (!File.Exists(tempFilePath))
                {
                    throw new ConflictCustomException("Failed to create temporary file for upload processing.");
                }

                // Đẩy xuống queue để tránh bị treo api
                BackgroundJob.Enqueue<IBackgoundService>(
                            x => x.CheckProgressingUploadsJob(userId, tempFilePath, createTrackRequest, createWorkRequest, createRecordingRequest)
                        );
            }
            else
            {
                // Tạm thời vẫn duyệt thủ công do cần kiểm duyệt legal document
                await AssignApproveManuallyAsync(autoStream, createTrackRequest, createWorkRequest, createRecordingRequest);
            }

            return true;
        }

        // Kiểm tra giấy phép bản quyền từ chủ sở hữu bản ghi gốc

        return true;
    }

    //internal async Task ApproveAutomaticallyAsync(Stream stream, CreateTrackRequest createTrackRequest, CreateWorkRequest createWorkRequest, CreateRecordingRequest createRecordingRequest)
    //{
    //    string outputHlsPath = string.Empty;
    //    WavFileResponse wavFileResponse = default!;
    //    try
    //    {
    //        // Duyệt tự động -> Lưu xuống database
    //        string tempName = ObjectId.GenerateNewId().ToString();

    //        //Console.WriteLine("===================================");
    //        //Console.WriteLine($"Temp Name: {tempName}");
    //        //Console.WriteLine("===================================");

    //        // Convert sang WAV
    //        AudioConvertPathOptions audioConvertPathOptionsWav = AudioConvertPathOptions.ForConvertToWav();

    //        // Convert file sang định dạng wav
    //        wavFileResponse = await _ffmpegService.ConvertToWavAsync(stream, tempName, audioConvertPathOptionsWav);

    //        //Console.WriteLine("===================================");
    //        //Console.WriteLine($"Wav Path: {wavFileResponse.OutputWavPath}");
    //        //Console.WriteLine($"File Exists? {File.Exists(wavFileResponse.OutputWavPath)}");
    //        //Console.WriteLine("===================================");

    //        // Tạo track temp
    //        TrackTempRequest trackTempRequest = _trackService.CreateTrackTemp(createTrackRequest);
    //        WorkTempRequest workTempRequest = _workService.CreateWorkTemp(createWorkRequest);
    //        RecordingTempRequest recordingTempRequest = _recordingService.CreateRecordingTemp(createRecordingRequest);

    //        // Tạo hls từ file wav
    //        AudioConvertPathOptions audioConvertPathOptionsHls = AudioConvertPathOptions.ForConvertToHls(trackTempRequest.Id);
    //        outputHlsPath = await _ffmpegService.ConvertToHlsAsync(wavFileResponse, audioConvertPathOptionsHls);

    //        //Console.WriteLine("===================================");
    //        //Console.WriteLine($"Wav Path: {wavFileResponse.OutputWavPath}");
    //        //Console.WriteLine("===================================");

    //        //AudioFingerprint audioFingerprint = await _audioFingerprintService.GenerateFingerprint(wavFileResponse);
    //        AudioFeature audioAnalysisResponse = await _audioAnalysisService.AnalyzeAudioAsync(wavFileResponse);

    //        // Xác định mood của track dựa trên đặc trưng âm thanh
    //        IEnumerable<MoodType> moodTypes = _categoryService.DetectMoods(audioAnalysisResponse);
    //        IEnumerable<string> moodCategoryIds = await _categoryService.GetMoodsFromAudioFeaturesAsync(moodTypes);

    //        string alternativeDescription = _categoryService.GenerateAlternativeDescription(audioAnalysisResponse, moodTypes);
    //        float[] embeddingVector = await _trackService.GenerateEmbeddingsAsync(alternativeDescription);

    //        TrackTempResponse trackTempResponse = new()
    //        {
    //            Id = trackTempRequest.Id,
    //            Name = trackTempRequest.Name,
    //            Description = trackTempRequest.Description,
    //            MainArtistIds = trackTempRequest.MainArtistIds,
    //            FeaturedArtistIds = trackTempRequest.FeaturedArtistIds,
    //            CategoryIds = trackTempRequest.CategoryIds.Concat(moodCategoryIds).ToList(),
    //            Tags = trackTempRequest.Tags,
    //            CoverImage = trackTempRequest.CoverImage,
    //            PreviewVideo = trackTempRequest.PreviewVideo,
    //            IsExplicit = trackTempRequest.IsExplicit,
    //            Lyrics = trackTempRequest.Lyrics,
    //            ReleaseInfo = trackTempRequest.ReleaseInfo,
    //            LegalDocuments = trackTempRequest.LegalDocuments,
    //            //AudioFingerprint = audioFingerprint,
    //            AudioFeature = audioAnalysisResponse,
    //            AlternativeDescription = alternativeDescription,
    //            EmbeddingVector = embeddingVector,

    //            CreatedBy = trackTempRequest.CreatedBy,
    //        };

    //        await _trackService.CreateTrackFromTrackUploadRequestAsync(trackTempResponse, workTempRequest, recordingTempRequest);

    //        // Upload original file to cloud storage (S3, GCP, Azure Blob, etc.)
    //        await _amazonS3Service.UploadOriginalAudioAsync(stream, trackTempResponse.Id, false);

    //        // Đẩy hls playlist lên S3
    //        await _amazonS3Service.UploadFolderAsync(outputHlsPath, trackTempRequest.Id);

    //        // Lưu snapshot
    //        // Track
    //        await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
    //        {
    //            TargetOwnerId = trackTempRequest.CreatedBy,
    //            TargetId = trackTempRequest.Id,
    //            ApprovalType = ApprovalType.TrackUpload,
    //            ActionByUserId = "68abf0fc5252e66631121e57",
    //            ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
    //            Action = HistoryActionType.Approved,
    //            Notes = null,
    //            Snapshot = trackTempRequest,
    //        });

    //        // Work
    //        await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
    //        {
    //            TargetId = workTempRequest.Id,
    //            ApprovalType = ApprovalType.WorkUpload,
    //            ActionByUserId = "68abf0fc5252e66631121e57",
    //            ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
    //            Action = HistoryActionType.Approved,
    //            Notes = null,
    //            Snapshot = workTempRequest,
    //        });

    //        // Recording
    //        await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
    //        {
    //            TargetId = recordingTempRequest.Id,
    //            ApprovalType = ApprovalType.RecordingUpload,
    //            ActionByUserId = "68abf0fc5252e66631121e57",
    //            ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
    //            Action = HistoryActionType.Approved,
    //            Notes = null,
    //            Snapshot = recordingTempRequest,
    //        });
    //    }
    //    finally
    //    {
    //        // Xóa folder, file tạm sau khi upload lên S3
    //        //HelperMethod.DeleteBatchIO(outputHlsPath, wavFileResponse.OutputWavPath);
    //        if (Directory.Exists(outputHlsPath))
    //        {
    //            Directory.Delete(outputHlsPath, true);
    //        }
    //        if (File.Exists(wavFileResponse.OutputWavPath))
    //        {
    //            File.Delete(wavFileResponse.OutputWavPath);
    //        }
    //    }
    //}

    internal async Task AssignApproveManuallyAsync(Stream stream, CreateTrackRequest createTrackRequest, CreateWorkRequest createWorkRequest, CreateRecordingRequest createRecordingRequest)
    {
        // Tạo track temp
        TrackTempRequest trackTemp = _trackService.CreateTrackTemp(createTrackRequest);
        WorkTempRequest workTemp = _workService.CreateWorkTemp(createWorkRequest);
        RecordingTempRequest recordingTemp = _recordingService.CreateRecordingTemp(createRecordingRequest);

        // Tạo combined upload request
        CombinedUploadRequest combinedRequest = new()
        {
            Id = trackTemp.Id, // Sử dụng trackId làm ID chính
            Track = trackTemp,
            Work = workTemp,
            Recording = recordingTemp,
            CreatedBy = createTrackRequest.CreatedByUserId!,
        };

        // Đẩy combined request lên redis để chờ duyệt (sử dụng 1 key thay vì 3 keys)
        await _redisCacheService.SetGenericAsync($"upload:{trackTemp.Id}:requestUpload", combinedRequest);

        // Upload original file to cloud storage (S3, GCP, Azure Blob, etc.)
        await _amazonS3Service.UploadOriginalAudioAsync(stream, trackTemp.Id, false);

        return;
    }

    //internal async Task<(double, string, string)> CheckTrackFingerprintAsync(Stream stream)
    //{
    //    AudioConvertPathOptions audioConvertPathOptions = AudioConvertPathOptions.ForConvertToWav();

    //    WavFileResponse wavFileResponse = await _ffmpegService.ConvertToWavAsync(stream, Guid.NewGuid().ToString(), audioConvertPathOptions);

    //    AudioFingerprintResult result = await _audioFingerprintService.GetMatchConfidenceScore(wavFileResponse);

    //    Console.WriteLine("===================================");
    //    Console.WriteLine($"Wav Path: {wavFileResponse.OutputWavPath}");
    //    Console.WriteLine("===================================");

    //    // Xóa file tạm sau khi nhận diện xong
    //    //HelperMethod.DeleteBatchIO(wavFileResponse.OutputWavPath);
    //    if (File.Exists(wavFileResponse.OutputWavPath))
    //    {
    //        File.Delete(wavFileResponse.OutputWavPath);
    //    }

    //    return (result.BestConfidence, result.TrackId, result.TrackName);
    //}

    public async Task<bool> RejectTrackUploadRequestAsync(string uploadId, string reasonReject, bool isCancled = false)
    {
        if (_redisCacheService.TryGetGeneric($"upload:{uploadId}:requestUpload", out CombinedUploadRequest? combinedRequest))
        {
            string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            if (combinedRequest is null)
            {
                throw new NotFoundCustomException("Upload request not found");
            }

            TrackTempRequest trackTempRequest = combinedRequest.Track;
            WorkTempRequest workTempRequest = combinedRequest.Work;
            RecordingTempRequest recordingTempRequest = combinedRequest.Recording;

            // Xóa file đã upload trên cloud storage
            await _amazonS3Service.DeleteOriginalAudioAsync(trackTempRequest.Id);

            // Xóa request trên redis (chỉ cần xóa 1 key)
            await _redisCacheService.RemoveAsync($"upload:{uploadId}:requestUpload");

            // Lưu snapshot
            // Track
            await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
            {
                TargetOwnerId = trackTempRequest.CreatedBy,
                TargetId = trackTempRequest.Id,
                ApprovalType = ApprovalType.TrackUpload,
                ActionByUserId = currentUserId,
                ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
                Action = isCancled ? HistoryActionType.Canceled : HistoryActionType.Rejected,
                Notes = reasonReject,
                Snapshot = trackTempRequest,
            });

            // Work
            await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
            {
                TargetId = workTempRequest.Id,
                ApprovalType = ApprovalType.WorkUpload,
                ActionByUserId = currentUserId,
                ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
                Action = isCancled ? HistoryActionType.Canceled : HistoryActionType.Rejected,
                Notes = reasonReject,
                Snapshot = workTempRequest,
            });

            // Recording
            await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
            {
                TargetId = recordingTempRequest.Id,
                ApprovalType = ApprovalType.RecordingUpload,
                ActionByUserId = currentUserId,
                ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
                Action = isCancled ? HistoryActionType.Canceled : HistoryActionType.Rejected,
                Notes = reasonReject,
                Snapshot = recordingTempRequest,
            });

            return true;
        }

        return false;
    }

    // Kiểm tra tự động: Audio file có định dạng hợp lệ, vi phạm chính sách không (bao gồm cả vi phạm bản quyền)
    public async Task<bool> ApproveTrackUploadRequestAsync(string uploadId)
    {
        string actionByUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        BackgroundJob.Enqueue<IBackgoundService>(
            x => x.CheckProgressingUploadsManuallyJob(actionByUserId, uploadId)
        );

        return true;
    }

    public async Task<bool> UpdateMetadataTrackAsync(UpdateTrackRequest updateTrackRequest)
    {
        await _trackService.UpdateMetadataTrackAsync(updateTrackRequest);
        return true;
    }

    public async Task<bool> AddToFavoriteTrackAsync(string trackId, bool isAdding, [Service] ITopicEventSender eventSender, CancellationToken cancellationToken)
    {
        long favoriteCountUpdated = await _trackService.AddToFavoriteTrackAsync(trackId, isAdding);
        //await eventSender.SendAsync(trackId, favoriteCountUpdated, cancellationToken);
        return true;
    }

    public async Task<bool> UpsertStreamCount(string trackId)
    {
        await _trackService.UpsertStreamCountAsync(trackId);
        return true;
    }
}
