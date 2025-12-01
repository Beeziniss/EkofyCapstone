using AutoMapper;
using EkofyApp.Application.Models.ApprovalHistories;
using EkofyApp.Application.Models.AudioFeatures;
using EkofyApp.Application.Models.Notifications;
using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Uploads;
using EkofyApp.Application.Models.Wavs;
using EkofyApp.Application.Models.Works;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.ApprovalHistories;
using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Application.ServiceInterfaces.Categories;
using EkofyApp.Application.ServiceInterfaces.Jobs;
using EkofyApp.Application.ServiceInterfaces.Recommendations;
using EkofyApp.Application.ServiceInterfaces.Recordings;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Application.ServiceInterfaces.Works;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Application.ThirdPartyServiceInterfaces.EmySound;
using EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using EkofyApp.Infrastructure.Services.Notifications;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;

namespace EkofyApp.Infrastructure.Services.Tracks;

public sealed class TrackService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IRedisCacheService redisCacheService, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, IRecommendationService recommendationService, IUserService userService, IFfmpegService ffmpegService, IWorkService workService, IRecordingService recordingService, ICategoryService categoryService, IAudioAnalysisService audioAnalysisService, IAmazonS3Service amazonS3Service, IApprovalHistoryService approvalHistoryService, IEmySoundService emySoundService, IArtistService artistService, ITrackUploadNotifier trackUploadNotifier, IHubContext<NotificationHub> hubContext, ILogger<TrackService> logger) : ITrackService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator = embeddingGenerator;
    private readonly IRecommendationService _recommendationService = recommendationService;
    private readonly IUserService _userService = userService;
    private readonly IFfmpegService _ffmpegService = ffmpegService;
    private readonly IWorkService _workService = workService;
    private readonly IRecordingService _recordingService = recordingService;
    private readonly ICategoryService _categoryService = categoryService;
    private readonly IAudioAnalysisService _audioAnalysisService = audioAnalysisService;
    private readonly IAmazonS3Service _amazonS3Service = amazonS3Service;
    private readonly IApprovalHistoryService _approvalHistoryService = approvalHistoryService;
    private readonly IEmySoundService _emySoundService = emySoundService;
    private readonly IArtistService _artistService = artistService;
    private readonly ITrackUploadNotifier _trackUploadNotifier = trackUploadNotifier;
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;
    private readonly ILogger<TrackService> _logger = logger;

    public async Task SeedMonthlyStreamCountByTrackIdAsync(string trackId, long streamCount, int month, int year)
    {
        UpdateResult updateTrackResult = await _unitOfWork.GetCollection<Track>()
            .UpdateOneAsync(x => x.Id == trackId, Builders<Track>.Update.Set(x => x.StreamCount, streamCount));

        if (updateTrackResult.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Cannot seed stream count.");
        }

        await _unitOfWork.GetCollection<MonthlyStreamCount>().InsertOneAsync(new MonthlyStreamCount
        {
            TrackId = trackId,
            StreamCount = streamCount,
            Month = month,
            Year = year,
        });
    }

    public IQueryable<Track> GetTracks()
    {
        return _unitOfWork.GetCollection<Track>().AsQueryable();
    }

    public IQueryable<Track> GetFavoriteTracks()
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        List<string> favoriteTrackIds = _unitOfWork.GetCollection<UserEngagement>()
            .Find(x => x.ActorId == userId && x.TargetType == UserEngagementTargetType.Track && x.Action == UserEngagementAction.Like)
            .Project(x => x.TargetId)
            .ToList();

        IQueryable<Track> query = _unitOfWork.GetCollection<Track>()
            .Find(x => favoriteTrackIds.Contains(x.Id))
            .ToEnumerable()
            .AsQueryable();

        return query;
    }

    public IQueryable<Track> SearchTracks(string name)
    {
        IQueryable<Track> query = _unitOfWork.GetCollection<Track>().AsQueryable();

        if (string.IsNullOrEmpty(name))
        {
            return query;
        }

        string unsignedSearchTerm = HelperMethod.ToUnsigned(name);
        query = query.Where(t => t.NameUnsigned.Contains(unsignedSearchTerm));

        return query;
    }

    public async Task ApproveAutomaticallyAsync(string userId, byte[] bytes, CreateTrackRequest createTrackRequest, CreateWorkRequest createWorkRequest, CreateRecordingRequest createRecordingRequest)
    {
        // Lấy stream từ bytes
        using MemoryStream stream = new(bytes);
        using MemoryStream emyStream = new(bytes);

        stream.Position = 0;
        emyStream.Position = 0;

        string outputHlsPath = string.Empty;
        WavFileResponse wavFileResponse = default!;
        try
        {
            // Duyệt tự động -> Lưu xuống database
            string tempName = ObjectId.GenerateNewId().ToString();

            // Convert sang WAV
            await _trackUploadNotifier.SendProgressAsync(userId, 10, "Processing audio file");
            //await Task.Delay(2000); // Giả lập delay cho người dùng thấy progress
            AudioConvertPathOptions audioConvertPathOptionsWav = AudioConvertPathOptions.ForConvertToWav();

            // Convert file sang định dạng wav
            wavFileResponse = await _ffmpegService.ConvertToWavAsync(stream, tempName, audioConvertPathOptionsWav);

            // Tạo track temp
            await _trackUploadNotifier.SendProgressAsync(userId, 20, "Processing track metadata");
            TrackTempRequest trackTempRequest = CreateTrackTemp(createTrackRequest);
            await _trackUploadNotifier.SendProgressAsync(userId, 25, "Processing work metadata");
            WorkTempRequest workTempRequest = _workService.CreateWorkTemp(createWorkRequest);
            await _trackUploadNotifier.SendProgressAsync(userId, 30, "Processing recording metadata");
            //await Task.Delay(2000); // Giả lập delay cho người dùng thấy progress
            RecordingTempRequest recordingTempRequest = _recordingService.CreateRecordingTemp(createRecordingRequest);

            // Tạo hls từ file wav
            await _trackUploadNotifier.SendProgressAsync(userId, 40, "Processing streaming audio file");
            //await Task.Delay(2000); // Giả lập delay cho người dùng thấy progress
            AudioConvertPathOptions audioConvertPathOptionsHls = AudioConvertPathOptions.ForConvertToHls(trackTempRequest.Id);
            outputHlsPath = await _ffmpegService.ConvertToHlsAsync(wavFileResponse, audioConvertPathOptionsHls);

            //AudioFingerprint audioFingerprint = await _audioFingerprintService.GenerateFingerprint(wavFileResponse);
            await _trackUploadNotifier.SendProgressAsync(userId, 50, "Analyzing audio file");
            //await Task.Delay(2000); // Giả lập delay cho người dùng thấy progress
            AudioFeature audioAnalysisResponse = await _audioAnalysisService.AnalyzeAudioAsync(wavFileResponse);

            // Xác định mood của track dựa trên đặc trưng âm thanh
            await _trackUploadNotifier.SendProgressAsync(userId, 60, "Extracting mood from audio file");
            //await Task.Delay(2000); // Giả lập delay cho người dùng thấy progress
            IEnumerable<MoodType> moodTypes = _categoryService.DetectMoods(audioAnalysisResponse);
            IEnumerable<string> moodCategoryIds = await _categoryService.GetMoodsFromAudioFeaturesAsync(moodTypes);

            await _trackUploadNotifier.SendProgressAsync(userId, 70, "Creating summary description from audio file");
            //await Task.Delay(2000); // Giả lập delay cho người dùng thấy progress
            string alternativeDescription = _categoryService.GenerateAlternativeDescription(audioAnalysisResponse, moodTypes);
            float[] embeddingVector = await GenerateEmbeddingsAsync(alternativeDescription);

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

            await _trackUploadNotifier.SendProgressAsync(userId, 80, "Uploading...");
            //await Task.Delay(5000); // Giả lập delay cho người dùng thấy progress

            await CreateTrackFromTrackUploadRequestAsync(trackTempResponse, workTempRequest, recordingTempRequest);

            // Upload original file to cloud storage (S3, GCP, Azure Blob, etc.)
            await _amazonS3Service.UploadOriginalAudioAsync(stream, trackTempResponse.Id, false);

            // Đẩy hls playlist lên S3
            await _amazonS3Service.UploadFolderAsync(outputHlsPath, trackTempRequest.Id);

            // Kiểm tra và lên lịch phát hành track nếu cần thiết
            await _trackUploadNotifier.SendProgressAsync(userId, 85, "Checking release schedule");
            //await Task.Delay(2000);
            if (ShouldScheduleTrackRelease(trackTempResponse.ReleaseInfo))
            {
                DateTimeOffset releaseTime = trackTempResponse.ReleaseInfo.ReleaseDate!.Value;
                BackgroundJob.Schedule<IBackgoundService>(
                    x => x.ReleaseScheduledTrackJob(trackTempResponse.Id),
                    releaseTime
                );
            }

            // Upload fingerprint lên EmySound
            await _trackUploadNotifier.SendProgressAsync(userId, 90, "Generating fingerprint");
            //await Task.Delay(2000);
            string stageName = await _artistService.GetArtistStageNameByUserIdAsync(trackTempResponse.CreatedBy);
            string trackId = await _emySoundService.UploadTrackFingerprintAsync(emyStream, trackTempRequest.Id, trackTempRequest.Name, stageName, trackTempRequest.CreatedByArtistId!) ?? throw new ConflictCustomException("There is an error while uploading track fingerprint.");

            // TODO: Xóa request trên redis và xóa tag trên S3 nếu có
            // Resolved: Đã xóa tag trên S3 và xóa request trên redis
            await _trackUploadNotifier.SendProgressAsync(userId, 95, "Cleaning up temporary resources");
            //await Task.Delay(1000);
            await _amazonS3Service.RemoveTagAsync(trackTempRequest.Id, [KeyTag.delete]);

            // Lưu snapshot
            //Track
            await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
            {
                TargetOwnerId = trackTempRequest.CreatedBy,
                TargetId = trackTempRequest.Id,
                ApprovalType = ApprovalType.TrackUpload,
                ActionByUserId = "68abf0fc5252e66631121e57",
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
                ActionByUserId = "68abf0fc5252e66631121e57",
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
                ActionByUserId = "68abf0fc5252e66631121e57",
                ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
                Action = HistoryActionType.Approved,
                Notes = null,
                Snapshot = recordingTempRequest,
            });

            await _trackUploadNotifier.SendProgressAsync(userId, 100, "Done");
            await _trackUploadNotifier.SendCompletedAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic approval process for userId: {UserId}", userId);
            await _trackUploadNotifier.SendFailedAsync(userId, "An error occurred during automatic approval process.");
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

    public async Task<bool> ApproveTrackUploadRequestAsync(string actionByUserId, string uploadId)
    {
        try
        {
            // Lưu xuống database
            if (_redisCacheService.TryGetGeneric($"upload:{uploadId}:requestUpload", out CombinedUploadRequest? combinedRequest))
            {
                WavFileResponse wavFileResponse = default!;

                if (combinedRequest is null)
                {
                    throw new NotFoundCustomException("Upload request not found");
                }

                TrackTempRequest trackTempRequest = combinedRequest.Track;
                WorkTempRequest workTempRequest = combinedRequest.Work;
                RecordingTempRequest recordingTempRequest = combinedRequest.Recording;

                // Tài nguyên từ S3
                await _trackUploadNotifier.SendProgressAsync(actionByUserId, 10, "Processing audio file");
                //await Task.Delay(2000); // Giả lập delay cho người dùng thấy progress
                await _amazonS3Service.DownloadOriginalAudioAsync(trackTempRequest.Id, async stream =>
                {
                    byte[] originalBytes;
                    using (MemoryStream originalStream = new())
                    {
                        await stream.CopyToAsync(originalStream);
                        originalBytes = originalStream.ToArray();
                    }

                    using MemoryStream ffmpegStream = new(originalBytes);
                    using MemoryStream emyStream = new(originalBytes);

                    ffmpegStream.Position = 0;
                    emyStream.Position = 0;

                    string tempName = ObjectId.GenerateNewId().ToString();

                    // Convert sang WAV
                    await _trackUploadNotifier.SendProgressAsync(actionByUserId, 20, "Processing audio file");
                    //await Task.Delay(500); // Giả lập delay cho người dùng thấy progress
                    AudioConvertPathOptions audioConvertPathOptionsWav = AudioConvertPathOptions.ForConvertToWav();

                    // Convert file sang định dạng wav
                    wavFileResponse = await _ffmpegService.ConvertToWavAsync(ffmpegStream, tempName, audioConvertPathOptionsWav);

                    // Tạo hls từ file wav
                    await _trackUploadNotifier.SendProgressAsync(actionByUserId, 30, "Processing streaming audio file");
                    //await Task.Delay(500);
                    AudioConvertPathOptions audioConvertPathOptionsHls = AudioConvertPathOptions.ForConvertToHls(trackTempRequest.Id);
                    string outputHlsPath = await _ffmpegService.ConvertToHlsAsync(wavFileResponse, audioConvertPathOptionsHls);

                    //AudioFingerprint audioFingerprint = await _audioFingerprintService.GenerateFingerprint(wavFileResponse);
                    await _trackUploadNotifier.SendProgressAsync(actionByUserId, 40, "Analyzing audio file");
                    //await Task.Delay(2000);
                    AudioFeature audioAnalysisResponse = await _audioAnalysisService.AnalyzeAudioAsync(wavFileResponse);

                    // Xác định mood của track dựa trên đặc trưng âm thanh
                    await _trackUploadNotifier.SendProgressAsync(actionByUserId, 50, "Extracting mood from audio file");
                    //await Task.Delay(2000);
                    IEnumerable<MoodType> moodTypes = _categoryService.DetectMoods(audioAnalysisResponse);
                    IEnumerable<string> moodCategoryIds = await _categoryService.GetMoodsFromAudioFeaturesAsync(moodTypes);

                    await _trackUploadNotifier.SendProgressAsync(actionByUserId, 60, "Creating summary description from audio file");
                    //await Task.Delay(2000);
                    string alternativeDescription = _categoryService.GenerateAlternativeDescription(audioAnalysisResponse, moodTypes);
                    float[] embeddingVector = await GenerateEmbeddingsAsync(alternativeDescription);

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

                    await _trackUploadNotifier.SendProgressAsync(actionByUserId, 70, "Processing metadata");
                    //await Task.Delay(2000);
                    await CreateTrackFromTrackUploadRequestAsync(trackTempResponse, workTempRequest, recordingTempRequest);

                    // Đẩy hls playlist lên S3
                    await _trackUploadNotifier.SendProgressAsync(actionByUserId, 80, "Uploading...");
                    //await Task.Delay(2000);
                    await _amazonS3Service.UploadFolderAsync(outputHlsPath, trackTempRequest.Id);

                    // Kiểm tra và lên lịch phát hành track nếu cần thiết
                    await _trackUploadNotifier.SendProgressAsync(actionByUserId, 85, "Checking release schedule");
                    //await Task.Delay(1000);
                    if (ShouldScheduleTrackRelease(trackTempRequest.ReleaseInfo))
                    {
                        DateTimeOffset releaseTime = trackTempRequest.ReleaseInfo.ReleaseDate!.Value;
                        BackgroundJob.Schedule<IBackgoundService>(
                            x => x.ReleaseScheduledTrackJob(trackTempRequest.Id),
                            releaseTime
                        );
                    }

                    // Upload fingerprint lên EmySound
                    await _trackUploadNotifier.SendProgressAsync(actionByUserId, 90, "Generating fingerprint");
                    //await Task.Delay(2000);
                    string stageName = await _artistService.GetArtistStageNameByUserIdAsync(trackTempRequest.CreatedBy);
                    string trackId = await _emySoundService.UploadTrackFingerprintAsync(emyStream, trackTempRequest.Id, trackTempRequest.Name, stageName, trackTempRequest.CreatedByArtistId!) ?? throw new ConflictCustomException("There is an error while uploading track fingerprint.");

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
                await _trackUploadNotifier.SendProgressAsync(actionByUserId, 95, "Cleaning up temporary resources");
                //await Task.Delay(1000);
                await _amazonS3Service.RemoveTagAsync(trackTempRequest.Id, [KeyTag.delete]);
                await _redisCacheService.RemoveAsync($"upload:{uploadId}:requestUpload");

                // Lưu snapshot
                // Track
                await _approvalHistoryService.CreateApprovalHistoryAsync(new ApprovalHistoryRequest
                {
                    TargetOwnerId = trackTempRequest.CreatedBy,
                    TargetId = trackTempRequest.Id,
                    ApprovalType = ApprovalType.TrackUpload,
                    ActionByUserId = actionByUserId,
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
                    ActionByUserId = actionByUserId,
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
                    ActionByUserId = actionByUserId,
                    ActionAt = HelperMethod.GetUtcPlus7TimeOffset(),
                    Action = HistoryActionType.Approved,
                    Notes = null,
                    Snapshot = recordingTempRequest,
                });

                await _trackUploadNotifier.SendProgressAsync(actionByUserId, 100, "Done");
                await _trackUploadNotifier.SendCompletedAsync(actionByUserId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving track upload request for uploadId: {UploadId}", uploadId);
            await _trackUploadNotifier.SendFailedAsync(actionByUserId, "An error occurred while approving the track upload request.");

            return false;
        }
    }

    private static bool ShouldScheduleTrackRelease(ReleaseInfo releaseInfo)
    {
        // Chỉ schedule job khi:
        // 1. IsRelease = false -> track được private thì không cần làm gì hết
        // Và không cần kiểm tra thêm 2 điều kiện còn lại
        // 2. IsRelease = true -> track được public -> Release Status -> Official -> không được chọn Release Date -> không cần schedule
        //                                                            -> Not Announced -> chọn Release Date để schedule -> cần schedule
        // 2.2 Sau khi chọn Release Date để schedule -> đến ngày thì track sẽ được release và Release Status sẽ được đổi thành Official

        //return releaseInfo.IsRelease &&
        //       releaseInfo.ReleaseStatus != ReleaseStatus.Official &&
        //       releaseInfo.ReleaseDate.HasValue;

        if (!releaseInfo.IsRelease)
        {
            return false;
        }

        if (releaseInfo.ReleaseStatus == ReleaseStatus.Official)
        {
            return false;
        }

        return releaseInfo.ReleaseDate.HasValue;
    }

    public async Task ReleaseScheduledTrackAsync(string trackId)
    {
        DateTimeOffset currentTime = HelperMethod.GetUtcPlus7TimeOffset();

        // Cập nhật thông tin phát hành track với điều kiện track chưa được release
        UpdateDefinition<Track> updateDefinition = Builders<Track>.Update
            .Set(t => t.ReleaseInfo.ReleasedAt, currentTime)
            .Set(t => t.ReleaseInfo.ReleaseStatus, ReleaseStatus.Official);

        FilterDefinition<Track> filter = Builders<Track>.Filter.And(
            Builders<Track>.Filter.Eq(t => t.Id, trackId),
            Builders<Track>.Filter.Eq(t => t.ReleaseInfo.ReleaseStatus, ReleaseStatus.NotAnnounced)
        );

        UpdateResult result = await _unitOfWork.GetCollection<Track>()
            .UpdateOneAsync(filter, updateDefinition);

        if (result.MatchedCount == 0)
        {
            throw new UnprocessableEntityCustomException($"Track {trackId} not found or already released. Skipping release job.");
        }

        if (result.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException($"Track {trackId} was not modified. It may have been released by another process.");
        }

        // Gửi notification
        Track track = await _unitOfWork.GetCollection<Track>()
            .Find(t => t.Id == trackId)
            .Project<Track>(Builders<Track>.Projection
                .Include(x => x.Id)
                .Include(x => x.Name)
                .Include(x => x.CreatedBy))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Artist for track {trackId} not found.");

        UserRole userRole = await _unitOfWork.GetCollection<User>()
            .Find(u => u.Id == track.CreatedBy)
            .Project(u => u.Role)
            .FirstOrDefaultAsync();

        NotificationUserInfo? notificationUserInfo = null;
        switch (userRole)
        {
            case UserRole.Listener:
                notificationUserInfo = await _unitOfWork.GetCollection<Listener>()
                    .Find(l => l.Id == track.CreatedBy)
                    .Project(x => new NotificationUserInfo
                    {
                        Name = x.DisplayName,
                        Avatar = x.AvatarImage ?? "https://res.cloudinary.com/dofnn7sbx/image/upload/v1730097883/60d5dc467b950c5ccc8ced95_spotify-for-artists_on4me9.jpg"
                    })
                    .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found user {track.CreatedBy}");

                break;
            case UserRole.Artist:
                notificationUserInfo = await _unitOfWork.GetCollection<Artist>()
                    .Find(a => a.Id == track.CreatedBy)
                    .Project(x => new NotificationUserInfo
                    {
                        Name = x.StageName,
                        Avatar = x.AvatarImage ?? "https://res.cloudinary.com/dofnn7sbx/image/upload/v1730097883/60d5dc467b950c5ccc8ced95_spotify-for-artists_on4me9.jpg"
                    })
                    .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found user {track.CreatedBy}");

                break;
        }

        IEnumerable<string> followerIds = _userService.GetFollowersByUserId(track.CreatedBy!).Select(x => x.Id);

        List<Notification> notifications = [];
        string content = HelperMethod.BuildContentNotification(NotificationActionType.Release, NotificationRelatedType.Track, track.Name, notificationUserInfo!.Name);
        foreach (string followerId in followerIds)
        {
            notifications.Add(new Notification
            {
                Id = ObjectId.GenerateNewId().ToString(),
                ActorId = track.CreatedBy!,
                TargetId = followerId,
                RelatedId = trackId,
                Content = content,
                RelatedType = NotificationRelatedType.Track,
                Action = NotificationActionType.Release,
            });
        }

        if (notifications.Count > 0)
        {
            await _unitOfWork.GetCollection<Notification>().InsertManyAsync(notifications);
            await _hubContext.Clients.Users(followerIds).SendAsync("ReceiveNotification", new NotificationResponse
            {
                Content = content,
                Avatar = notificationUserInfo!.Avatar,
            });
        }
    }

    public async Task CreateTrackFromTrackUploadRequestAsync(TrackTempResponse trackResponse, WorkTempRequest workTempRequest, RecordingTempRequest recordingTempRequest)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            Track track = new()
            {
                Id = trackResponse.Id,
                Name = trackResponse.Name,
                NameUnsigned = HelperMethod.ToUnsigned(trackResponse.Name),
                Description = trackResponse.Description,

                Type = trackResponse.Type,

                MainArtistIds = trackResponse.MainArtistIds,
                FeaturedArtistIds = trackResponse.FeaturedArtistIds,
                CategoryIds = trackResponse.CategoryIds,
                Tags = trackResponse.Tags,

                CoverImage = trackResponse.CoverImage,
                PreviewVideo = trackResponse.PreviewVideo,

                //AudioFingerprint = trackResponse.AudioFingerprint,
                AudioFeature = trackResponse.AudioFeature,
                AlternativeDescription = trackResponse.AlternativeDescription,
                EmbeddingVector = trackResponse.EmbeddingVector,

                IsExplicit = trackResponse.IsExplicit,
                Lyrics = trackResponse.Lyrics,

                ReleaseInfo = trackResponse.ReleaseInfo,
                Restriction = new()
                {
                    Type = RestrictionType.None,
                },

                LegalDocuments = trackResponse.LegalDocuments,

                CreatedBy = trackResponse.CreatedBy,
            };

            Work work = new()
            {
                Id = workTempRequest.Id,
                TrackId = trackResponse.Id,

                Description = workTempRequest.Description,
                WorkSplits = _mapper.Map<List<WorkSplit>>(workTempRequest.WorkSplits),
                Version = 1,
                Status = WorkStatus.Active,
            };

            Recording recording = new()
            {
                Id = recordingTempRequest.Id,
                TrackId = trackResponse.Id,

                Description = recordingTempRequest.Description,
                RecordingSplits = _mapper.Map<List<RecordingSplit>>(recordingTempRequest.RecordingSplitRequests),
                Version = 1,
                Status = RecordingStatus.Active,
            };

            await _unitOfWork.GetCollection<Track>().InsertOneAsync(session, track);
            await _unitOfWork.GetCollection<Work>().InsertOneAsync(session, work);
            await _unitOfWork.GetCollection<Recording>().InsertOneAsync(session, recording);
        });
    }

    public TrackTempRequest CreateTrackTemp(CreateTrackRequest createTrackRequest)
    {
        // Workaround for tránh trùng userId khi tạo track
        createTrackRequest.MainArtistIds.Add(createTrackRequest.CreatedByArtistId!);
        createTrackRequest.MainArtistIds = createTrackRequest.MainArtistIds.Distinct().ToList();

        TrackTempRequest track = new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = createTrackRequest.Name,
            Description = createTrackRequest.Description,

            CreatedByArtistId = createTrackRequest.CreatedByArtistId,
            MainArtistIds = createTrackRequest.MainArtistIds,
            FeaturedArtistIds = createTrackRequest.FeaturedArtistIds,
            CategoryIds = createTrackRequest.CategoryIds,
            Tags = createTrackRequest.Tags,

            CoverImage = createTrackRequest.CoverImage,
            PreviewVideo = createTrackRequest.PreviewVideo,
            IsExplicit = createTrackRequest.IsExplicit,
            Lyrics = createTrackRequest.Lyrics,

            ReleaseInfo = new()
            {
                IsRelease = createTrackRequest.IsRelease,
                ReleaseDate = createTrackRequest.ReleaseDate,
                ReleaseStatus = createTrackRequest.ReleaseStatus,
            },

            LegalDocuments = createTrackRequest.LegalDocuments,

            CreatedBy = createTrackRequest.CreatedByUserId!,
        };

        return track;
    }

    private async Task<PaginatedData<CombinedUploadRequest>> GetPendingTrackUploadRequestsAsync(int pageNumber = 1, int pageSize = 20)
    {
        ICacheResult<PaginatedData<CombinedUploadRequest>> result = await _redisCacheService.GetPendingCombinedUploadsAsync(pageNumber, pageSize);

        PaginatedData<CombinedUploadRequest> paginatedData;

        if (!result.Success || result.Value == null)
        {
            return new PaginatedData<CombinedUploadRequest>
            {
                Items = [],
                TotalCount = 0
            };
        }

        paginatedData = new()
        {
            Items = result.Value.Items,
            TotalCount = result.Value.TotalCount
        };

        return paginatedData;
    }

    private async Task<CombinedUploadRequest> GetPendingTrackUploadRequestByUploadIdAsync(string uploadId)
    {
        ICacheResult<CombinedUploadRequest> cacheResult = await _redisCacheService.TryGetGenericAsync<CombinedUploadRequest>($"upload:{uploadId}:requestUpload");

        if (!cacheResult.Success || cacheResult.Value == null)
        {
            throw new NotFoundCustomException($"Upload request with ID {uploadId} not found or expired.");
        }

        return cacheResult.Value;
    }

    public async Task EscalateOldUploadRequestsAsync()
    {
        try
        {
            // Get all pending upload requests
            PaginatedData<CombinedUploadRequest> allPendingRequests = await GetPendingTrackUploadRequestsAsync(1, int.MaxValue);

            if (allPendingRequests.Items == null || !allPendingRequests.Items.Any())
            {
                return;
            }

            DateTimeOffset currentTime = HelperMethod.GetUtcPlus7TimeOffset();
            DateTimeOffset threeDaysAgo = currentTime.AddDays(-3);

            // Find requests older than 3 days that can be escalated
            List<CombinedUploadRequest> requestsToEscalate = allPendingRequests.Items
                .Where(request => request.RequestedAt <= threeDaysAgo &&
                                request.ApprovalPriority != ApprovalPriorityStatus.Urgent)
                .ToList();

            if (requestsToEscalate.Count == 0)
            {
                return;
            }

            int escalatedCount = 0;

            foreach (CombinedUploadRequest request in requestsToEscalate)
            {
                try
                {
                    ApprovalPriorityStatus? newPriority = GetNextPriorityLevel(request.ApprovalPriority);

                    if (newPriority.HasValue)
                    {
                        await UpdateUploadRequestPriorityAsync(request.Id, newPriority.Value);
                        escalatedCount++;

                        //_logger.LogInformation(
                        //    "Escalated upload request {UploadId} from {OldPriority} to {NewPriority}. Request age: {Age} days",
                        //    request.Id, 
                        //    request.ApprovalPriority, 
                        //    newPriority.Value,
                        //    (currentTime - request.RequestedAt).Days);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to escalate upload request {UploadId}", request.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during upload request escalation process");
            throw;
        }
    }

    private static ApprovalPriorityStatus? GetNextPriorityLevel(ApprovalPriorityStatus currentPriority)
    {
        return currentPriority switch
        {
            ApprovalPriorityStatus.Low => ApprovalPriorityStatus.Medium,
            ApprovalPriorityStatus.Medium => ApprovalPriorityStatus.High,
            ApprovalPriorityStatus.High => ApprovalPriorityStatus.Urgent,
            ApprovalPriorityStatus.Urgent => null, // Already at highest level
            _ => null
        };
    }

    private async Task UpdateUploadRequestPriorityAsync(string uploadId, ApprovalPriorityStatus newPriority)
    {
        try
        {
            // Get the current request from Redis
            ICacheResult<CombinedUploadRequest> cacheResult = await _redisCacheService.TryGetGenericAsync<CombinedUploadRequest>($"upload:{uploadId}:requestUpload");

            if (!cacheResult.Success || cacheResult.Value == null)
            {
                _logger.LogError("Upload request {UploadId} not found in cache during priority update", uploadId);
                return;
            }

            CombinedUploadRequest currentRequest = cacheResult.Value;

            // Create updated request with new priority
            CombinedUploadRequest updatedRequest = currentRequest with
            {
                ApprovalPriority = newPriority
            };

            // Update the request in Redis (maintain the same TTL)
            TimeSpan? currentTtl = await _redisCacheService.GetTTLAsync($"upload:{uploadId}:requestUpload");
            TimeSpan ttlToSet = currentTtl ?? TimeSpan.FromDays(7); // Default 7 days if no TTL found

            await _redisCacheService.SetGenericAsync($"upload:{uploadId}:requestUpload", updatedRequest, ttlToSet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update priority for upload request {UploadId}", uploadId);
            throw;
        }
    }

    public async Task<PaginatedData<CombinedUploadRequest>> GetPendingTrackUploadRequestsAsync(
        string? userId = null,
        ApprovalPriorityStatus? priority = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        try
        {
            // Get all pending requests
            PaginatedData<CombinedUploadRequest> allRequests = await GetPendingTrackUploadRequestsAsync(1, int.MaxValue);

            if (allRequests.Items == null || !allRequests.Items.Any())
            {
                return new PaginatedData<CombinedUploadRequest>
                {
                    Items = [],
                    TotalCount = 0
                };
            }

            // Filter by priority if specified
            IEnumerable<CombinedUploadRequest> filteredItems = allRequests.Items;

            if (priority.HasValue)
            {
                filteredItems = filteredItems.Where(r => r.ApprovalPriority == priority.Value);
            }

            // Filter by userId if specified
            if (!string.IsNullOrEmpty(userId))
            {
                filteredItems = filteredItems.Where(r => r.CreatedBy == userId);
            }

            // Sort by priority (Urgent first) and then by request date (oldest first)
            List<CombinedUploadRequest> sortedItems = filteredItems
                .OrderByDescending(r => r.ApprovalPriority)
                .ThenBy(r => r.RequestedAt)
                .ToList();

            // Apply pagination
            List<CombinedUploadRequest> paginatedItems = sortedItems
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PaginatedData<CombinedUploadRequest>
            {
                Items = paginatedItems,
                TotalCount = sortedItems.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending upload requests by priority");
            throw;
        }
    }

    public async Task<CombinedUploadRequest> GetPendingTrackUploadRequestByUploadIdAsync(string uploadId, ApprovalPriorityStatus? priority = null)
    {
        try
        {
            // Get the specific upload request by ID
            CombinedUploadRequest uploadRequest = await GetPendingTrackUploadRequestByUploadIdAsync(uploadId);

            // Check if priority filter should be applied
            if (priority.HasValue && uploadRequest.ApprovalPriority != priority.Value)
            {
                throw new NotFoundCustomException($"Upload request with ID {uploadId} and priority {priority} not found.");
            }

            return uploadRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending upload request by ID with priority filter");
            throw;
        }
    }

    public async Task UpdateMetadataTrackAsync(UpdateTrackRequest updateTrackRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // Verify that the track exists and the user has permission to edit it
        Track existingTrack = await _unitOfWork.GetCollection<Track>()
            .Find(t => t.Id == updateTrackRequest.TrackId)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Track with ID {updateTrackRequest.TrackId} not found.");

        // Check if user is authorized to update this track (owner or main artist)
        if (existingTrack.CreatedBy != userId && !existingTrack.MainArtistIds.Contains(userId))
        {
            throw new UnauthorizedCustomException("You don't have permission to update this track.");
        }

        // Build update definition
        UpdateDefinitionBuilder<Track> updateDefinitionBuilder = Builders<Track>.Update;
        List<UpdateDefinition<Track>> updates = [];

        // Update description if provided
        if (!string.IsNullOrEmpty(updateTrackRequest.Description))
        {
            updates.Add(updateDefinitionBuilder.Set(t => t.Description, updateTrackRequest.Description));
        }

        // Update category IDs if provided
        if (updateTrackRequest.CategoryIds != null && updateTrackRequest.CategoryIds.Count > 0)
        {
            updates.Add(updateDefinitionBuilder.Set(t => t.CategoryIds, updateTrackRequest.CategoryIds));
        }

        // Update tags if provided
        if (updateTrackRequest.Tags != null && updateTrackRequest.Tags.Count > 0)
        {
            updates.Add(updateDefinitionBuilder.Set(t => t.Tags, updateTrackRequest.Tags));
        }

        // Update timestamp and user info
        updates.Add(updateDefinitionBuilder.Set(t => t.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));
        updates.Add(updateDefinitionBuilder.Set(t => t.UpdatedBy, userId));

        if (updates.Count <= 2) // Only UpdatedAt and UpdatedBy were added
        {
            throw new BadRequestCustomException("No valid fields provided for update.");
        }

        // Combine all updates
        UpdateDefinition<Track> combinedUpdate = updateDefinitionBuilder.Combine(updates);

        // Execute update
        UpdateResult updateResult = await _unitOfWork.GetCollection<Track>()
            .UpdateOneAsync(t => t.Id == updateTrackRequest.TrackId, combinedUpdate);
        if (updateResult.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Failed to update track metadata.");
        }
    }

    #region Favorite Tracks
    public async Task<long> AddToFavoriteTrackAsync(string trackId, bool isAdding)
    {
        long updatedFavoriteCount = isAdding ? 1 : -1;

        Track trackUpdated = await _unitOfWork.GetCollection<Track>()
        .FindOneAndUpdateAsync(t => t.Id == trackId, Builders<Track>.Update.Inc(t => t.FavoriteCount, updatedFavoriteCount),
        new FindOneAndUpdateOptions<Track>
        {
            // Trả về tài liệu sau khi cập nhật
            ReturnDocument = ReturnDocument.After,
            Projection = Builders<Track>.Projection
                .Include(t => t.Id)
                .Include(t => t.FavoriteCount)
        }) ?? throw new NotFoundCustomException($"Track with ID {trackId} not found.");

        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
        string role = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // Nếu isAdding false, tức là người dùng bỏ thích bài hát
        if (!isAdding)
        {
            // Xóa track khỏi cache yêu thích của users
            await RemoveTrackFromFavoriteCacheAsync(userId, trackId);
            return trackUpdated.FavoriteCount;
        }

        // Thêm track yêu thích của users vào UserEngagement
        await _unitOfWork.GetCollection<UserEngagement>()
            .InsertOneAsync(new UserEngagement
            {
                ActorId = userId,
                ActorType = Enum.Parse<UserRole>(role) == UserRole.Listener ? UserEngagementTargetType.Listener : UserEngagementTargetType.Artist,
                TargetId = trackId,
                TargetType = UserEngagementTargetType.Track,
                Action = UserEngagementAction.Like,
            });

        // Thêm track yêu thích của users vào cache
        await AddTrackToFavoriteCacheAsync(userId, trackId);

        // Trả về số lượt yêu thích mới của bài hát
        return trackUpdated.FavoriteCount;
    }

    public async Task<bool> CheckTrackInFavoriteAsync(string trackId)
    {
        //string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string? userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        try
        {
            string cacheKey = $"favorite_track:{userId}";

            // Check if cache exists and has items
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);

            if (listLength > 0)
            {
                // Cache hit - check if track exists in Redis list
                return await _redisCacheService.ListContainsAsync(cacheKey, trackId);
            }

            // Cache miss - populate from database
            await EnsureCachePopulatedAsync(userId);

            // Check again after population
            return await _redisCacheService.ListContainsAsync(cacheKey, trackId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if track {TrackId} is in favorite for user {UserId}", trackId, userId);
            return false;
        }
    }

    private async Task AddTrackToFavoriteCacheAsync(string userId, string trackId)
    {
        try
        {
            string cacheKey = $"favorite_track:{userId}";

            // Check if cache exists
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);

            if (listLength > 0)
            {
                // Cache exists - check if track already exists to avoid duplicates
                bool exists = await _redisCacheService.ListContainsAsync(cacheKey, trackId);

                if (!exists)
                {
                    // Get current TTL to preserve it
                    var remainingTtl = await _redisCacheService.GetTTLAsync(cacheKey);
                    var ttlToSet = remainingTtl ?? TimeSpan.FromHours(1);

                    // Add track to the beginning of the list
                    await _redisCacheService.ListPushAsync(cacheKey, trackId, ttlToSet);
                }
            }
            else
            {
                // Cache doesn't exist - create new list with this track
                await _redisCacheService.ListPushAsync(cacheKey, trackId, TimeSpan.FromHours(1));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add track {TrackId} to favorite cache for user {UserId}", trackId, userId);
        }
    }

    private async Task RemoveTrackFromFavoriteCacheAsync(string userId, string trackId)
    {
        try
        {
            string cacheKey = $"favorite_track:{userId}";

            // Check if cache exists
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);

            if (listLength > 0)
            {
                // Get current TTL to preserve it
                var remainingTtl = await _redisCacheService.GetTTLAsync(cacheKey);

                // Remove track from list (removes all occurrences)
                long removedCount = await _redisCacheService.ListRemoveAsync(cacheKey, trackId, 0);

                if (removedCount > 0)
                {
                    // Restore TTL if there are still items in the list
                    long newLength = await _redisCacheService.ListLengthAsync(cacheKey);
                    if (newLength > 0 && remainingTtl.HasValue)
                    {
                        await _redisCacheService.SetExpirationAsync(cacheKey, remainingTtl);
                    }

                    _logger.LogDebug("Removed track {TrackId} from favorite cache for user {UserId}", trackId, userId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove track {TrackId} from favorite cache for user {UserId}", trackId, userId);
        }
    }

    private async Task InvalidateFavoriteCacheAsync(string userId)
    {
        try
        {
            string cacheKey = $"favorite_track:{userId}";
            await _redisCacheService.RemoveAsync(cacheKey);

            _logger.LogDebug("Invalidated favorite cache for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate favorite cache for user {UserId}", userId);
        }
    }

    private async Task<bool> EnsureCachePopulatedAsync(string userId)
    {
        try
        {
            string cacheKey = $"favorite_track:{userId}";

            // Check if cache already exists
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);
            if (listLength > 0)
            {
                return true; // Cache already populated
            }

            // Fetch favorite track from database
            List<string> favoriteTrackIds = await _unitOfWork.GetCollection<UserEngagement>()
                .Find(x => x.ActorId == userId && x.TargetType == UserEngagementTargetType.Track && x.Action == UserEngagementAction.Like)
                .Project(x => x.TargetId)
                .ToListAsync();

            if (favoriteTrackIds.Count > 0)
            {
                // Populate cache with track IDs
                await _redisCacheService.ListPushRangeAsync(cacheKey, favoriteTrackIds, TimeSpan.FromHours(1));

                _logger.LogDebug("Populated favorite cache for user {UserId} with {Count} tracks", userId, favoriteTrackIds.Count);
                return true;
            }

            _logger.LogDebug("No favorite tracks found for user {UserId}", userId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to populate favorite cache for user {UserId}", userId);
            return false;
        }
    }
    #endregion

    public async Task UpsertStreamCountAsync(string trackId)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string key = $"stream_count:{userId}";  //--> cái cũ là top track ??
                                                //tăng lượt stream count lên 1 khi được gọi
        await _redisCacheService.HashIncrementAsync(key, trackId);
        //set thời gian tồn tại của key trong 30'
        await _redisCacheService.SetExpirationAsync(key, TimeSpan.FromMinutes(3));
    }

    //NOTE: Hàm để chạy duyệt qua các track chưa có embedding chứ ko phải 1 track
    public async Task AddEmbeddingVectorAsync()
    {
        try
        {
            IEnumerable<Track> tracks = await _unitOfWork.GetCollection<Track>()
            .Find(t => t.Description != null)
            .ToListAsync();

            //lọc ra các track chưa có embedding
            var trackWithoutEmbedding = tracks
                                        .Where(t => t.EmbeddingVector is null or { Length: 0 })
                                        .ToList();


            var embedding = new Dictionary<string, float[]>();

            //lặp qua các track chưa có embedding và tạo vector cho từng track
            foreach (var track in trackWithoutEmbedding)
            {
                //nối description và alternative description
                string totalDescription = (track.Description ?? string.Empty) + ". " + track.AlternativeDescription;
                if (!embedding.ContainsKey(totalDescription))
                {
                    embedding[track.Id] = await GenerateEmbeddingsAsync(totalDescription);
                }
            }

            //update tất cả các track chưa có embedding
            var updates = new List<UpdateOneModel<Track>>();
            foreach (var track in trackWithoutEmbedding)
            {
                var filter = Builders<Track>.Filter.Eq(t => t.Id, track.Id);
                var update = Builders<Track>.Update.Set(t => t.EmbeddingVector, embedding[track.Id]);
                updates.Add(new UpdateOneModel<Track>(filter, update));

            }

            if (updates.Any())
            {
                await _unitOfWork.GetCollection<Track>().BulkWriteAsync(updates);
            }
        }
        catch (Exception e)
        {
            throw new BadRequestCustomException(e.Message);
        }
    }

    public async Task<float[]> GenerateEmbeddingsAsync(string term)
    {
        var generatedEmbeddings = await _embeddingGenerator.GenerateAsync([term]);
        var embedding = generatedEmbeddings.Single();
        return embedding.Vector.ToArray();
    }

    //NOTE: Hàm tìm kiếm track theo semantic
    public async Task<IEnumerable<Track>> GetAllTracksBySemanticAsync(string text, int limit = 20)
    {
        //nếu text rỗng thì trả về track nhu bình thường
        if (string.IsNullOrEmpty(text))
        {
            return GetTracks();
        }

        //tạo vector từ text để tí so sánh
        var embedding = await GenerateEmbeddingsAsync(text);

        var vectorSearchOptions = new VectorSearchOptions<Track>
        {
            IndexName = "vector_index",
            //lấy 150 vector gần giống để so sánh
            NumberOfCandidates = 150,
        };

        return await _unitOfWork.GetCollection<Track>()
            .Aggregate()
            .VectorSearch(track => track.EmbeddingVector, embedding, limit, vectorSearchOptions)
            .Project<Track>(Builders<Track>.Projection
                .Exclude(t => t.EmbeddingVector)
                .Exclude(t => t.AudioFeature)
                .Exclude(t => t.AlternativeDescription))
            .ToListAsync();
    }

    public IQueryable<Track> GetEuclideanRecommendedTracksByTrackId(string trackId, AudioFeatureWeight audioFeatureWeight, int limit = 10)
    {
        AudioFeature audioFeature = _unitOfWork.GetCollection<Track>()
            .Find(t => t.Id == trackId)
            .Project(t => t.AudioFeature)
            .FirstOrDefault() ?? throw new NotFoundCustomException($"Track with ID {trackId} not found.");

        return _recommendationService.GetEuclideanRecommendedTracks(audioFeature, audioFeatureWeight, limit);
    }

    public IQueryable<Track> GetCosineRecommendedTracksByTrackId(string trackId, AudioFeatureWeight audioFeatureWeight, int limit = 10)
    {
        AudioFeature audioFeature = _unitOfWork.GetCollection<Track>()
            .Find(t => t.Id == trackId)
            .Project(t => t.AudioFeature)
            .FirstOrDefault() ?? throw new NotFoundCustomException($"Track with ID {trackId} not found.");

        return _recommendationService.GetCosineRecommendedTracks(audioFeature, audioFeatureWeight, limit);
    }

    #region Không đụng đến
    public async Task<TrackResponse> GetTrackResolverContext(ProjectionDefinition<Track> projection, string id)
    {
        Track track = await _unitOfWork.GetCollection<Track>()
            .Find(x => x.Id == id)
            .Project<Track>(projection)
            .FirstOrDefaultAsync();

        return _mapper.Map<TrackResponse>(track);
    }
    #endregion
}
