using AutoMapper;
using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Works;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Artist;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Tracks;

public sealed class TrackService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IRedisCacheService redisCacheService ) : ITrackService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;

    public IQueryable<Track> GetTracksQueryable()
    {
        return _unitOfWork.GetCollection<Track>().AsQueryable();
    }

    public async Task<TrackResponse> GetTrackResolverContext(ProjectionDefinition<Track> projection, string id)
    {
        Track track = await _unitOfWork.GetCollection<Track>()
            .Find(x => x.Id == id)
            .Project<Track>(projection)
            .FirstOrDefaultAsync();

        return _mapper.Map<TrackResponse>(track);
    }

    public async Task CreateTrackFromTrackUploadRequestAsync(TrackTempResponse trackResponse, WorkTempRequest workTempRequest, RecordingTempRequest recordingTempRequest)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            Track track = new()
            {
                Id = trackResponse.Id,
                Name = trackResponse.Name,
                Description = trackResponse.Description,

                Type = trackResponse.Type,

                MainArtistIds = trackResponse.MainArtistIds,
                FeaturedArtistIds = trackResponse.FeaturedArtistIds,
                CategoryIds = trackResponse.CategoryIds,
                Tags = trackResponse.Tags,

                CoverImage = trackResponse.CoverImage,
                PreviewVideo = trackResponse.PreviewVideo,

                AudioFingerprint = trackResponse.AudioFingerprint,
                AudioFeature = trackResponse.AudioFeature,

                IsExplicit = trackResponse.IsExplicit,
                Lyrics = trackResponse.Lyrics,

                ReleaseInfo = trackResponse.ReleaseInfo,
                Restriction = new()
                {
                    Type = RestrictionType.None,
                },

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
        string artistId = _httpContextAccessor.HttpContext?.User.FindFirst("artistId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // Workaround for tránh trùng userId khi tạo track
        createTrackRequest.MainArtistIds.Add(artistId);
        createTrackRequest.MainArtistIds = createTrackRequest.MainArtistIds.Distinct().ToList();

        TrackTempRequest track = new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = createTrackRequest.Name,
            Description = createTrackRequest.Description,

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
                IsReleased = createTrackRequest.IsReleased,
                ReleaseDate = createTrackRequest.ReleaseDate,
                ReleaseStatus = createTrackRequest.ReleaseStatus,
            },

            CreatedBy = artistId,
        };

        return track;
    }

    public async Task UpdateStreamCount(string trackId)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string key =  $"stream_count:{userId}";  //--> cái cũ là top track ??
        //tăng lượt stream count lên 1 khi được gọi
        await _redisCacheService.HashIncrementAsync(key, trackId);
        //set thời gian tồn tại của key trong 30'
        await _redisCacheService.SetExpirationAsync(key, TimeSpan.FromMinutes(3));
    }
}
