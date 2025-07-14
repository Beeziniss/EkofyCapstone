using AutoMapper;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Tracks;
public sealed class TrackGraphQLService(IUnitOfWork unitOfWork, IMapper mapper, IRedisCacheService redisCacheService) : ITrackGraphQLService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;

    public IQueryable<TrackResponse> GetTracksQueryable()
    {
        IQueryable<Track> queryable = _unitOfWork.GetCollection<Track>().AsQueryable();
        return _mapper.ProjectTo<TrackResponse>(queryable);
        //return queryable.Select(track => new TrackResponse
        //{
        //    Id = track.Id,
        //    Name = track.Name,
        //    Description = track.Description,
        //    ArtistId = track.ArtistId,
        //});

        // Method ProjectTo sử dụng Expression<Func<Tracks, TrackResponse>>
        // Nên HotChocolate không thể hiểu hoặc kiểm soát được projection này
        // Do đó [UseProjection] sẽ bị bỏ qua 
    }

    public IQueryable<Track> GetTracksQueryableDB()
    {
        return _unitOfWork.GetCollection<Track>().AsQueryable();
    }
}
