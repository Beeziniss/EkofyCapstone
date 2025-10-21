using EkofyApp.Application.Models.ApprovalHistories;
using EkofyApp.Application.Models.AudioFingerprints;
using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Wavs;
using EkofyApp.Application.Models.Works;
using EkofyApp.Application.ServiceInterfaces.ApprovalHistories;
using EkofyApp.Application.ServiceInterfaces.Categories;
using EkofyApp.Application.ServiceInterfaces.Recordings;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ServiceInterfaces.Works;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Application.ThirdPartyServiceInterfaces.EmySound;
using EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using HotChocolate.Subscriptions;
using MongoDB.Bson;
using Refit;

namespace EkofyApp.Api.GraphQL.Mutation.Tracks;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class TrackMutation(ITrackService trackService, IRedisCacheService redisCacheService, IAmazonS3Service amazonS3Service, IFfmpegService ffmpegService, IAudioAnalysisService audioAnalysisService, ICategoryService categoryService, IWorkService workService, IRecordingService recordingService, IEmySoundService emySoundService, IApprovalHistoryService approvalHistoryService, IHttpContextAccessor httpContextAccessor)
{
    private readonly ITrackService _trackService = trackService;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;
    private readonly IAmazonS3Service _amazonS3Service = amazonS3Service;
    private readonly IFfmpegService _ffmpegService = ffmpegService;
    private readonly IAudioAnalysisService _audioAnalysisService = audioAnalysisService;
    private readonly ICategoryService _categoryService = categoryService;
    private readonly IWorkService _workService = workService;
    private readonly IRecordingService _recordingService = recordingService;
    private readonly IEmySoundService _emySoundService = emySoundService;
    private readonly IApprovalHistoryService _approvalHistoryService = approvalHistoryService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<bool> UploadTrackAsync(IFile file, CreateTrackRequest createTrackRequest, CreateWorkRequest createWorkRequest, CreateRecordingRequest createRecordingRequest)
    {
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

            IEnumerable<QueryAudioFingerprintResponse> responses = await _emySoundService.CheckTrackFingerprintAsync(streamPart);
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

            // Duyệt tự động
            //await ApproveAutomaticallyAsync(stream, createTrackRequest, createWorkRequest, createRecordingRequest);

            // Tạo stream mới để dùng cho duyệt tự động
            using var autoStream = new MemoryStream(fileBytes);
            // Tạm thời vẫn duyệt thủ công do cần kiểm duyệt legal document
            await AssignApproveManuallyAsync(autoStream, createTrackRequest, createWorkRequest, createRecordingRequest);

            return true;
        }

        // Kiểm tra giấy phép bản quyền từ chủ sở hữu bản ghi gốc

        return true;
    }

    internal async Task ApproveAutomaticallyAsync(Stream stream, CreateTrackRequest createTrackRequest, CreateWorkRequest createWorkRequest, CreateRecordingRequest createRecordingRequest)
    {
        string outputHlsPath = string.Empty;
        WavFileResponse wavFileResponse = default!;
        try
        {
            // Duyệt tự động -> Lưu xuống database
            string tempName = ObjectId.GenerateNewId().ToString();

            //Console.WriteLine("===================================");
            //Console.WriteLine($"Temp Name: {tempName}");
            //Console.WriteLine("===================================");

            // Convert sang WAV
            AudioConvertPathOptions audioConvertPathOptionsWav = AudioConvertPathOptions.ForConvertToWav();

            // Convert file sang định dạng wav
            wavFileResponse = await _ffmpegService.ConvertToWavAsync(stream, tempName, audioConvertPathOptionsWav);

            //Console.WriteLine("===================================");
            //Console.WriteLine($"Wav Path: {wavFileResponse.OutputWavPath}");
            //Console.WriteLine($"File Exists? {File.Exists(wavFileResponse.OutputWavPath)}");
            //Console.WriteLine("===================================");

            // Tạo track temp
            TrackTempRequest trackTempRequest = _trackService.CreateTrackTemp(createTrackRequest);
            WorkTempRequest workTempRequest = _workService.CreateWorkTemp(createWorkRequest);
            RecordingTempRequest recordingTempRequest = _recordingService.CreateRecordingTemp(createRecordingRequest);

            // Tạo hls từ file wav
            AudioConvertPathOptions audioConvertPathOptionsHls = AudioConvertPathOptions.ForConvertToHls(trackTempRequest.Id);
            outputHlsPath = await _ffmpegService.ConvertToHlsAsync(wavFileResponse, audioConvertPathOptionsHls);

            //Console.WriteLine("===================================");
            //Console.WriteLine($"Wav Path: {wavFileResponse.OutputWavPath}");
            //Console.WriteLine("===================================");

            //AudioFingerprint audioFingerprint = await _audioFingerprintService.GenerateFingerprint(wavFileResponse);
            AudioFeature audioAnalysisResponse = await _audioAnalysisService.AnalyzeAudioAsync(wavFileResponse);

            // Xác định mood của track dựa trên đặc trưng âm thanh
            IEnumerable<MoodType> moodTypes = _categoryService.DetectMoods(audioAnalysisResponse);
            IEnumerable<string> moodCategoryIds = await _categoryService.GetMoodsFromAudioFeaturesAsync(moodTypes);

            string alternativeDescription = _categoryService.GenerateAlternativeDescription(audioAnalysisResponse, moodTypes);
            float[] embeddingVector = await _trackService.GenerateEmbeddingsAsync(alternativeDescription);

            TrackTempResponse trackTempResponse = new()
            {
                Id = trackTempRequest.Id,
                Name = trackTempRequest.Name,
                Description = trackTempRequest.Description,
                MainArtistIds = trackTempRequest.MainArtistIds,
                FeaturedArtistIds = trackTempRequest.FeaturedArtistIds,
                CategoryIds = trackTempRequest.CategoryIds.Concat(moodCategoryIds).ToList(),
                Tags = trackTempRequest.Tags,
                CoverImage = trackTempRequest.CoverImage,
                PreviewVideo = trackTempRequest.PreviewVideo,
                IsExplicit = trackTempRequest.IsExplicit,
                Lyrics = trackTempRequest.Lyrics,
                ReleaseInfo = trackTempRequest.ReleaseInfo,
                LegalDocuments = trackTempRequest.LegalDocuments,
                //AudioFingerprint = audioFingerprint,
                AudioFeature = audioAnalysisResponse,
                AlternativeDescription = alternativeDescription,
                EmbeddingVector = embeddingVector,

                CreatedBy = trackTempRequest.CreatedBy,
            };

            await _trackService.CreateTrackFromTrackUploadRequestAsync(trackTempResponse, workTempRequest, recordingTempRequest);

            // Upload original file to cloud storage (S3, GCP, Azure Blob, etc.)
            await _amazonS3Service.UploadOriginalAudioAsync(stream, trackTempResponse.Id, false);

            // Đẩy hls playlist lên S3
            await _amazonS3Service.UploadFolderAsync(outputHlsPath, trackTempRequest.Id);

            // Lưu snapshot
            // Track
            await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
            {
                TargetOwnerId = trackTempRequest.CreatedBy,
                TargetId = trackTempRequest.Id,
                ApprovalType = ApprovalType.TrackUpload,
                ActionByUserId = "System",
                ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
                Action = HistoryActionType.Approved,
                Notes = null,
                Snapshot = trackTempRequest,
            });

            // Work
            await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
            {
                TargetId = workTempRequest.Id,
                ApprovalType = ApprovalType.WorkUpload,
                ActionByUserId = "System",
                ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
                Action = HistoryActionType.Approved,
                Notes = null,
                Snapshot = workTempRequest,
            });

            // Recording
            await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
            {
                TargetId = recordingTempRequest.Id,
                ApprovalType = ApprovalType.RecordingUpload,
                ActionByUserId = "System",
                ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
                Action = HistoryActionType.Approved,
                Notes = null,
                Snapshot = recordingTempRequest,
            });
        }
        finally
        {
            // Xóa folder, file tạm sau khi upload lên S3
            //HelperMethod.DeleteBatchIO(outputHlsPath, wavFileResponse.OutputWavPath);
            if (Directory.Exists(outputHlsPath))
            {
                Directory.Delete(outputHlsPath, true);
            }
            if (File.Exists(wavFileResponse.OutputWavPath))
            {
                File.Delete(wavFileResponse.OutputWavPath);
            }
        }
    }

    internal async Task AssignApproveManuallyAsync(Stream stream, CreateTrackRequest createTrackRequest, CreateWorkRequest createWorkRequest, CreateRecordingRequest createRecordingRequest)
    {
        // Tạo track temp
        TrackTempRequest trackTemp = _trackService.CreateTrackTemp(createTrackRequest);
        WorkTempRequest workTemp = _workService.CreateWorkTemp(createWorkRequest);
        RecordingTempRequest recordingTemp = _recordingService.CreateRecordingTemp(createRecordingRequest);

        // Đẩy request lên redis để chờ duyệt
        await _redisCacheService.SetGenericAsync($"track:{trackTemp.Id}:requestUpload", trackTemp, TimeSpan.FromDays(3));
        await _redisCacheService.SetGenericAsync($"work:{workTemp.Id}:requestUpload", workTemp, TimeSpan.FromDays(3));
        await _redisCacheService.SetGenericAsync($"recording:{recordingTemp.Id}:requestUpload", recordingTemp, TimeSpan.FromDays(3));

        // Upload original file to cloud storage (S3, GCP, Azure Blob, etc.)
        await _amazonS3Service.UploadOriginalAudioAsync(stream, trackTemp.Id);

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

    public async Task<bool> RejectTrackUploadRequestAsync(string trackId, string workId, string recordingId, string reasonReject)
    {
        if (_redisCacheService.TryGetGeneric($"track:{trackId}:requestUpload", out TrackTempRequest? trackTempRequest) &&
            _redisCacheService.TryGetGeneric($"work:{workId}:requestUpload", out WorkTempRequest? workTempRequest) &&
            _redisCacheService.TryGetGeneric($"recording:{recordingId}:requestUpload", out RecordingTempRequest? recordingTempRequest))
        {
            string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            if (trackTempRequest is null)
            {
                throw new NotFoundCustomException("Track upload request not found");
            }
            if (workTempRequest is null)
            {
                throw new NotFoundCustomException("WorkProjection upload request not found");
            }
            if (recordingTempRequest is null)
            {
                throw new NotFoundCustomException("RecordingProjection upload request not found");
            }

            // Xóa file đã upload trên cloud storage
            await _amazonS3Service.DeleteOriginalAudioAsync(trackTempRequest.Id);

            // Xóa request trên redis
            await _redisCacheService.RemoveAsync($"track:{trackId}:requestUpload");
            await _redisCacheService.RemoveAsync($"work:{workId}:requestUpload");
            await _redisCacheService.RemoveAsync($"recording:{recordingId}:requestUpload");

            // Lưu snapshot
            // Track
            await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
            {
                TargetOwnerId = trackTempRequest.CreatedBy,
                TargetId = trackTempRequest.Id,
                ApprovalType = ApprovalType.TrackUpload,
                ActionByUserId = currentUserId,
                ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
                Action = HistoryActionType.Rejected,
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
                Action = HistoryActionType.Rejected,
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
                Action = HistoryActionType.Rejected,
                Notes = reasonReject,
                Snapshot = recordingTempRequest,
            });

            return true;
        }

        return false;
    }

    // Kiểm tra tự động: Audio file có định dạng hợp lệ, vi phạm chính sách không (bao gồm cả vi phạm bản quyền)
    public async Task<bool> ApproveTrackUploadRequestAsync(string trackId, string workId, string recordingId)
    {
        // Lưu xuống database
        if (_redisCacheService.TryGetGeneric($"track:{trackId}:requestUpload", out TrackTempRequest? trackTempRequest) &&
            _redisCacheService.TryGetGeneric($"work:{workId}:requestUpload", out WorkTempRequest? workTempRequest) &&
            _redisCacheService.TryGetGeneric($"recording:{recordingId}:requestUpload", out RecordingTempRequest? recordingTempRequest))
        {
            string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            WavFileResponse wavFileResponse = default!;

            if (trackTempRequest is null)
            {
                throw new NotFoundCustomException("Track upload request not found");
            }
            if (workTempRequest is null)
            {
                throw new NotFoundCustomException("WorkProjection upload request not found");
            }
            if (recordingTempRequest is null)
            {
                throw new NotFoundCustomException("RecordingProjection upload request not found");
            }

            // Tài nguyên từ S3
            await _amazonS3Service.DownloadOriginalAudioAsync(trackTempRequest.Id, async stream =>
            {
                string tempName = ObjectId.GenerateNewId().ToString();

                // Convert sang WAV
                AudioConvertPathOptions audioConvertPathOptionsWav = AudioConvertPathOptions.ForConvertToWav();

                // Convert file sang định dạng wav
                wavFileResponse = await _ffmpegService.ConvertToWavAsync(stream, tempName, audioConvertPathOptionsWav);

                // Tạo hls từ file wav
                AudioConvertPathOptions audioConvertPathOptionsHls = AudioConvertPathOptions.ForConvertToHls(trackTempRequest.Id);
                string outputHlsPath = await _ffmpegService.ConvertToHlsAsync(wavFileResponse, audioConvertPathOptionsHls);

                //AudioFingerprint audioFingerprint = await _audioFingerprintService.GenerateFingerprint(wavFileResponse);
                AudioFeature audioAnalysisResponse = await _audioAnalysisService.AnalyzeAudioAsync(wavFileResponse);

                // Xác định mood của track dựa trên đặc trưng âm thanh
                IEnumerable<MoodType> moodTypes = _categoryService.DetectMoods(audioAnalysisResponse);
                IEnumerable<string> moodCategoryIds = await _categoryService.GetMoodsFromAudioFeaturesAsync(moodTypes);

                string alternativeDescription = _categoryService.GenerateAlternativeDescription(audioAnalysisResponse, moodTypes);
                float[] embeddingVector = await _trackService.GenerateEmbeddingsAsync(alternativeDescription);

                TrackTempResponse trackTempResponse = new()
                {
                    Id = trackTempRequest.Id,
                    Name = trackTempRequest.Name,
                    Description = trackTempRequest.Description,
                    MainArtistIds = trackTempRequest.MainArtistIds,
                    FeaturedArtistIds = trackTempRequest.FeaturedArtistIds,
                    CategoryIds = trackTempRequest.CategoryIds.Concat(moodCategoryIds).ToList(),
                    Tags = trackTempRequest.Tags,
                    CoverImage = trackTempRequest.CoverImage,
                    PreviewVideo = trackTempRequest.PreviewVideo,
                    IsExplicit = trackTempRequest.IsExplicit,
                    Lyrics = trackTempRequest.Lyrics,
                    ReleaseInfo = trackTempRequest.ReleaseInfo,

                    //AudioFingerprint = audioFingerprint,
                    AudioFeature = audioAnalysisResponse,
                    AlternativeDescription = alternativeDescription,
                    EmbeddingVector = embeddingVector,

                    CreatedBy = trackTempRequest.CreatedBy,
                };

                await _trackService.CreateTrackFromTrackUploadRequestAsync(trackTempResponse, workTempRequest, recordingTempRequest);

                // Đẩy hls playlist lên S3
                await _amazonS3Service.UploadFolderAsync(outputHlsPath, trackTempRequest.Id);

                // Upload fingerprint lên EmySound
                if (stream.CanSeek && stream.Position != 0)
                {
                    //stream.Seek(0, SeekOrigin.Begin);
                    stream.Position = 0;
                }

                string trackId = await _emySoundService.UploadTrackFingerprintAsync(stream, trackTempRequest.Id, trackTempRequest.Name, trackTempRequest.CreatedBy) ?? throw new ConflictCustomException("There is an error while uploading track fingerprint.");

                // Xóa folder, file tạm sau khi upload lên S3
                //HelperMethod.DeleteBatchIO(outputHlsPath, wavFileResponse.OutputWavPath);
                if (Directory.Exists(outputHlsPath))
                {
                    Directory.Delete(outputHlsPath, true);
                }
                if (File.Exists(wavFileResponse.OutputWavPath))
                {
                    File.Delete(wavFileResponse.OutputWavPath);
                }
            });

            // TODO: Xóa request trên redis và xóa tag trên S3 nếu có
            // Resolved: Đã xóa tag trên S3 và xóa request trên redis
            await _amazonS3Service.RemoveTagAsync(trackTempRequest.Id, [KeyTag.delete]);
            await _redisCacheService.RemoveAsync($"track:{trackId}:requestUpload");
            await _redisCacheService.RemoveAsync($"work:{workId}:requestUpload");
            await _redisCacheService.RemoveAsync($"recording:{recordingId}:requestUpload");

            // Lưu snapshot
            // Track
            await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
            {
                TargetOwnerId = trackTempRequest.CreatedBy,
                TargetId = trackTempRequest.Id,
                ApprovalType = ApprovalType.TrackUpload,
                ActionByUserId = currentUserId,
                ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
                Action = HistoryActionType.Approved,
                Notes = null,
                Snapshot = trackTempRequest,
            });

            // Work
            await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
            {
                TargetId = workTempRequest.Id,
                ApprovalType = ApprovalType.WorkUpload,
                ActionByUserId = currentUserId,
                ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
                Action = HistoryActionType.Approved,
                Notes = null,
                Snapshot = workTempRequest,
            });

            // Recording
            await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
            {
                TargetId = recordingTempRequest.Id,
                ApprovalType = ApprovalType.RecordingUpload,
                ActionByUserId = currentUserId,
                ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
                Action = HistoryActionType.Approved,
                Notes = null,
                Snapshot = recordingTempRequest,
            });

            return true;
        }

        return false;
    }

    public async Task<bool> UpdateFavoriteCountAsync(string trackId, bool isAdding, [Service] ITopicEventSender eventSender, CancellationToken cancellationToken)
    {
        long incrementValue = isAdding ? 1 : -1;
        long favoriteCountUpdated = await _trackService.UpdateFavoriteCountAsync(trackId, incrementValue);
        await eventSender.SendAsync(trackId, favoriteCountUpdated, cancellationToken);
        return true;
    }
}
