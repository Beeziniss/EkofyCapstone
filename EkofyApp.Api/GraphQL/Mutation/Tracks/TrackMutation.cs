using EkofyApp.Application.Models.AudioFingerprints;
using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Wavs;
using EkofyApp.Application.Models.Works;
using EkofyApp.Application.ServiceInterfaces.Categories;
using EkofyApp.Application.ServiceInterfaces.Recordings;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ServiceInterfaces.Works;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using MongoDB.Bson;

namespace EkofyApp.Api.GraphQL.Mutation.Tracks
{
    [ExtendObjectType(typeof(MutationInitialization))]
    [MutationType]
    public sealed class TrackMutation(ITrackService trackService, IRedisCacheService redisCacheService, IAmazonS3Service amazonS3Service, IAudioFingerprintService audioFingerprintService, IFfmpegService ffmpegService, IAudioAnalysisService audioAnalysisService, ICategoryService categoryService, IWorkService workService, IRecordingService recordingService)
    {
        private readonly ITrackService _trackService = trackService;
        private readonly IRedisCacheService _redisCacheService = redisCacheService;
        private readonly IAmazonS3Service _amazonS3Service = amazonS3Service;
        private readonly IAudioFingerprintService _audioFingerprintService = audioFingerprintService;
        private readonly IFfmpegService _ffmpegService = ffmpegService;
        private readonly IAudioAnalysisService _audioAnalysisService = audioAnalysisService;
        private readonly ICategoryService _categoryService = categoryService;
        private readonly IWorkService _workService = workService;
        private readonly IRecordingService _recordingService = recordingService;

        public async Task<bool> UploadTrackAsync(IFile file, CreateTrackRequest createTrackRequest, CreateWorkRequest createWorkRequest, CreateRecordingRequest createRecordingRequest)
        {
            using Stream stream = file.OpenReadStream();

            if (createTrackRequest.IsOriginal)
            {
                // Kiểm tra bản quyền
                (double confidence, string trackId, string trackName) = await CheckTrackFingerprintAsync(stream);
                stream.Position = 0;

                switch (confidence)
                {
                    case > 0.8:
                        {
                            throw new BadRequestCustomException($"The uploaded track is likely to infringe copyright.\nScore: {confidence}.\nTrack: {trackId} | {trackName}.");
                        }

                    case > 0.7:
                        {
                            // Đánh cờ duyệt thủ công
                            // TODO: Kiểm tra các thông tin metadata cơ bản tự động
                            // Như định dạng file, bitrate, sample rate, duration, v.v.
                            // Lyrics có explicit không: nếu có thì track phải đánh dấu explicit
                            // Còn nếu không có thì track không được đánh dấu explicit
                            // Trường hợp không đánh dấu explicit mà lyrics có từ ngữ nhạy cảm thì sẽ tự động set explicit là true

                            await ApproveManuallyAsync(stream, createTrackRequest, createWorkRequest, createRecordingRequest);
                            stream.Position = 0;

                            return true;
                        }

                    default:
                        {
                            await ApproveAutomaticallyAsync(stream, createTrackRequest, createWorkRequest, createRecordingRequest);
                            stream.Position = 0;

                            return true;
                        }
                }
            }
            else
            {
                // Kiểm tra giấy phép bản quyền từ chủ sở hữu bản ghi gốc

            }

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

                AudioFingerprint audioFingerprint = await _audioFingerprintService.GenerateFingerprint(wavFileResponse);
                AudioFeature audioAnalysisResponse = await _audioAnalysisService.AnalyzeAudioAsync(wavFileResponse);

                // Xác định mood của track dựa trên đặc trưng âm thanh
                IEnumerable<string> moodCategoryIds = await _categoryService.GetMoodsFromAudioFeaturesAsync(audioAnalysisResponse);

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
                    AudioFingerprint = audioFingerprint,
                    AudioFeature = audioAnalysisResponse,
                    CreatedBy = trackTempRequest.CreatedBy,
                };

                await _trackService.CreateTrackFromTrackUploadRequestAsync(trackTempResponse, workTempRequest, recordingTempRequest);

                // Upload original file to cloud storage (S3, GCP, Azure Blob, etc.)
                await _amazonS3Service.UploadOriginalAudioAsync(stream, trackTempResponse.Id, false);

                // Đẩy hls playlist lên S3
                await _amazonS3Service.UploadFolderAsync(outputHlsPath, trackTempRequest.Id);
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

        internal async Task ApproveManuallyAsync(Stream stream, CreateTrackRequest createTrackRequest, CreateWorkRequest createWorkRequest, CreateRecordingRequest createRecordingRequest)
        {
            // Tạo track temp
            TrackTempRequest trackTemp = _trackService.CreateTrackTemp(createTrackRequest);
            WorkTempRequest workTemp = _workService.CreateWorkTemp(createWorkRequest);
            RecordingTempRequest recordingTemp = _recordingService.CreateRecordingTemp(createRecordingRequest);

            // Đẩy request lên redis để chờ duyệt
            await _redisCacheService.SetAsync($"track:{trackTemp.Id}:requestUpload", trackTemp, TimeSpan.FromDays(3));
            await _redisCacheService.SetAsync($"work:{workTemp.Id}:requestUpload", workTemp, TimeSpan.FromDays(3));
            await _redisCacheService.SetAsync($"recording:{recordingTemp.Id}:requestUpload", recordingTemp, TimeSpan.FromDays(3));

            // Upload original file to cloud storage (S3, GCP, Azure Blob, etc.)
            await _amazonS3Service.UploadOriginalAudioAsync(stream, trackTemp.Id);

            return;
        }

        internal async Task<(double, string, string)> CheckTrackFingerprintAsync(Stream stream)
        {
            AudioConvertPathOptions audioConvertPathOptions = AudioConvertPathOptions.ForConvertToWav();

            WavFileResponse wavFileResponse = await _ffmpegService.ConvertToWavAsync(stream, Guid.NewGuid().ToString(), audioConvertPathOptions);

            AudioFingerprintResult result = await _audioFingerprintService.GetMatchConfidenceScore(wavFileResponse);

            Console.WriteLine("===================================");
            Console.WriteLine($"Wav Path: {wavFileResponse.OutputWavPath}");
            Console.WriteLine("===================================");

            // Xóa file tạm sau khi nhận diện xong
            //HelperMethod.DeleteBatchIO(wavFileResponse.OutputWavPath);
            if(File.Exists(wavFileResponse.OutputWavPath))
            {
                File.Delete(wavFileResponse.OutputWavPath);
            }

            return (result.BestConfidence, result.TrackId, result.TrackName);
        }

        public async Task<bool> RejectTrackUploadRequestAsync(string trackId, string workId, string recordingId)
        {
            if (_redisCacheService.TryGet($"track:{trackId}:requestUpload", out TrackTempRequest? trackUploadRequest) &&
                await _redisCacheService.ExistsAsync($"work:{workId}:requestUpload") &&
                await _redisCacheService.ExistsAsync($"recording:{recordingId}:requestUpload"))
            {
                if (trackUploadRequest is null)
                {
                    throw new NotFoundCustomException("Track upload request not found");
                }

                // Xóa file đã upload trên cloud storage
                await _amazonS3Service.DeleteOriginalAudioAsync(trackUploadRequest.Id);

                // Xóa request trên redis
                await _redisCacheService.RemoveAsync($"track:{trackId}:requestUpload");
                await _redisCacheService.RemoveAsync($"work:{workId}:requestUpload");
                await _redisCacheService.RemoveAsync($"recording:{recordingId}:requestUpload");

                return true;
            }

            return false;
        }

        // Kiểm tra tự động: Audio file có định dạng hợp lệ, vi phạm chính sách không (bao gồm cả vi phạm bản quyền)
        public async Task<bool> ApproveTrackUploadRequestAsync(string trackId, string workId, string recordingId)
        {
            // Lưu xuống database
            if (_redisCacheService.TryGet($"track:{trackId}:requestUpload", out TrackTempRequest? trackTempRequest) &&
                _redisCacheService.TryGet($"work:{workId}:requestUpload", out WorkTempRequest? workTempRequest) &&
                _redisCacheService.TryGet($"recording:{recordingId}:requestUpload", out RecordingTempRequest? recordingTempRequest))
            {
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
                });

                // Tạo hls từ file wav
                AudioConvertPathOptions audioConvertPathOptionsHls = AudioConvertPathOptions.ForConvertToHls(trackTempRequest.Id);
                string outputHlsPath = await _ffmpegService.ConvertToHlsAsync(wavFileResponse, audioConvertPathOptionsHls);

                AudioFingerprint audioFingerprint = await _audioFingerprintService.GenerateFingerprint(wavFileResponse);
                AudioFeature audioAnalysisResponse = await _audioAnalysisService.AnalyzeAudioAsync(wavFileResponse);

                // Xác định mood của track dựa trên đặc trưng âm thanh
                IEnumerable<string> moodCategoryIds = await _categoryService.GetMoodsFromAudioFeaturesAsync(audioAnalysisResponse);

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
                    AudioFingerprint = audioFingerprint,
                    AudioFeature = audioAnalysisResponse,
                    CreatedBy = trackTempRequest.CreatedBy,
                };

                await _trackService.CreateTrackFromTrackUploadRequestAsync(trackTempResponse, workTempRequest, recordingTempRequest);

                // Đẩy hls playlist lên S3
                await _amazonS3Service.UploadFolderAsync(outputHlsPath, trackTempRequest.Id);

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

                // TODO: Xóa request trên redis và xóa tag trên S3 nếu có
                // Resolved: Đã xóa tag trên S3 và xóa request trên redis
                await _amazonS3Service.RemoveTagAsync(trackTempRequest.Id, [KeyTag.delete]);
                await _redisCacheService.RemoveAsync($"track:{trackId}:requestUpload");
                await _redisCacheService.RemoveAsync($"work:{workId}:requestUpload");
                await _redisCacheService.RemoveAsync($"recording:{recordingId}:requestUpload");

                return true;
            }

            return false;
        }
    }
}
