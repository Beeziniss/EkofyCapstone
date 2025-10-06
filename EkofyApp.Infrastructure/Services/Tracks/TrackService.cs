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
using Microsoft.Extensions.AI;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Tracks;

public sealed class TrackService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IRedisCacheService redisCacheService, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) : ITrackService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator = embeddingGenerator;

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

                //AudioFingerprint = trackResponse.AudioFingerprint,
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
                    embedding[track.Id] = await GenerateEmbeddings(totalDescription);
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

    private async Task<float[]> GenerateEmbeddings(string term)
    {
        var generatedEmbeddings = await _embeddingGenerator.GenerateAsync([term]);
        var embedding = generatedEmbeddings.Single();
        return embedding.Vector.ToArray();
    }

    //NOTE: Hàm tìm kiếm track theo semantic
    public async Task<IEnumerable<Track>> GetAllTracksBySemanticAsync(string text,int limit = 20)
    {
        //nếu text rỗng thì trả về track nhu bình thường
        if (string.IsNullOrEmpty(text))
        {
            return GetTracksQueryable();
        }

        //tạo vector từ text để tí so sánh
        var embedding = await GenerateEmbeddings(text);

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

    private async Task AddOneEmbeddingVectorAsync(string trackId)
    {
        try
        {
            Track track = await _unitOfWork.GetCollection<Track>()
                                .Find(t => t.Id == trackId && t.EmbeddingVector == null)
                                .FirstOrDefaultAsync();

            if (track is null)
            {
                return;
            }


            var embedding = new Dictionary<string, float[]>();


            //nối description và alternative description roòi tạo vector
            string totalDescription = (track.Description ?? string.Empty) + ". " + track.AlternativeDescription;
            if (!embedding.ContainsKey(totalDescription))
            {
                embedding[track.Id] = await GenerateEmbeddings(totalDescription);
            }


            //update track chưa có embedding
            var filter = Builders<Track>.Filter.Eq(t => t.Id, track.Id);
            var update = Builders<Track>.Update.Set(t => t.EmbeddingVector, embedding[track.Id]);


            await _unitOfWork.GetCollection<Track>().UpdateOneAsync(filter, update);

        }
        catch (Exception e)
        {
            throw new BadRequestCustomException(e.Message);
        }

    }
}
