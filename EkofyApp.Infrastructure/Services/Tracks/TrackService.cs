using AutoMapper;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Tracks;

public sealed class TrackService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor) : ITrackService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

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

    public async Task CreateTrackFromTrackUploadRequestAsync(TrackTempResponse trackResponse)
    {
        Track track = new()
        {
            Id = trackResponse.Id,
            Name = trackResponse.Name,
            Description = trackResponse.Description,

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

            CreatedBy = trackResponse.CreatedBy,
        };

        await _unitOfWork.GetCollection<Track>().InsertOneAsync(track);
    }

    public TrackTempRequest CreateTrackTemp(CreateTrackRequest createTrackRequest)
    {
        string artistId = _httpContextAccessor.HttpContext?.User.FindFirst("artistId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // Workaround for tránh trùng artistId khi tạo track
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
}
