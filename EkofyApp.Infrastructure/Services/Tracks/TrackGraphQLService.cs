using AutoMapper;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Domain.Entities;
using HealthyNutritionApp.Application.Interfaces;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Tracks;
public class TrackGraphQLService(IUnitOfWork unitOfWork, IMapper mapper) : ITrackGraphQLService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;

    public IQueryable<TrackResponse> GetTracksExecutable()
    {
        IQueryable<Track> queryable = _unitOfWork.GetCollection<Track>().AsQueryable();
        return _mapper.ProjectTo<TrackResponse>(queryable);
        //return queryable.Select(track => new TrackResponse
        //{
        //    Id = track.Id,
        //    Name = track.Name,
        //    Description = track.Description
        //});

        // Method ProjectTo sử dụng Expression<Func<Tracks, TrackResponse>>
        // Nên HotChocolate không thể hiểu hoặc kiểm soát được projection này
        // Do đó [UseProjection] sẽ bị bỏ qua 
    }

    public IQueryable<Track> GetTracksQueryable()
    {
        return _unitOfWork.GetCollection<Track>().AsQueryable();
    }
}
