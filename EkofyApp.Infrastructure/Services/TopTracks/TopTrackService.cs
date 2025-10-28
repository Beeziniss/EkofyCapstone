using EkofyApp.Application.Models.TopTracks;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.TopTracks;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.TopTracks
{
    public sealed class TopTrackService(IUnitOfWork unitOfWork, IRedisCacheService redis, IHttpContextAccessor httpContextAccessor) : ITopTrackService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IRedisCacheService _redis = redis;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public IQueryable<TopTrackResponse> GetTopTracksByUserId()
        {
            // Lấy userId từ HttpContext
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            // Trả về các top tracks với user id tương ứng
            return _unitOfWork.GetCollection<TopTrack>()
                .AsQueryable()
                .Where(tt => tt.UserId == userId)
                .Select(t => new TopTrackResponse
                {
                    TracksInfo = t.TracksInfo
                .OrderByDescending(x => x.PlayedCount)
                .ToList()
                });
        }

        //public async Task AddPlayedTrackCountAsync(string trackId)
        //{
        //    // Lấy userId từ HttpContext
        //    string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
        //    // lưu /tăng số lần chơi của track trong Redis
        //    await _redis.HashIncrementAsync($"top_tracks:{userId}", trackId, 1);
        //    //set TTL cho key là 3 phút
        //    await _redis.SetExpirationAsync($"top_tracks:{userId}", TimeSpan.FromMinutes(3));
        //}

        public async Task UpsertTopTrackCountAsync(string trackId, CancellationToken cancellationToken = default)
        {
            // Lấy userId từ HttpContext
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
                            ?? throw new UnauthorizedCustomException("Your session is limited");

            var collection = _unitOfWork.GetCollection<TopTrack>();

            // kiểm tra xem đã có record TopTrack cho user này chưa
            var existing = await collection.Find(x => x.UserId == userId).FirstOrDefaultAsync(cancellationToken);

            if (existing == null)
            {
                //chưa có TopTrack => tạo mới
                var newTopTrack = new TopTrack
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    UserId = userId,
                    TracksInfo = new List<TopTrackInfo>
                    {
                        new TopTrackInfo { TrackId = trackId, PlayedCount = 1 }
                    }
                };

                await collection.InsertOneAsync(newTopTrack, cancellationToken: cancellationToken);
                return;
            }

            // nếu có, kiểm tra trackId đã tồn tại trong TracksInfo chưa
            var trackInfo = existing.TracksInfo.FirstOrDefault(t => t.TrackId == trackId);

            if (trackInfo != null)
            {
                // Nếu có, tăng Count
                var filter = Builders<TopTrack>.Filter.And(
                    Builders<TopTrack>.Filter.Eq(x => x.UserId, userId),
                    Builders<TopTrack>.Filter.Eq("TracksInfo.TrackId", trackId)
                );

                var update = Builders<TopTrack>.Update.Inc("TracksInfo.$.PlayedCount", 1);

                await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            }
            else
            {
                // Nếu chưa có track này, thêm mới vào list TracksInfo
                var filter = Builders<TopTrack>.Filter.Eq(x => x.UserId, userId);
                var update = Builders<TopTrack>.Update.Push(x => x.TracksInfo, new TopTrackInfo
                {
                    TrackId = trackId,
                    PlayedCount = 1
                });

                await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            }
        }
    }
}
