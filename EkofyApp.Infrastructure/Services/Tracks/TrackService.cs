using AutoMapper;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Domain.Entities;
using HealthyNutritionApp.Application.Interfaces;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Tracks
{
    public class TrackService(IUnitOfWork unitOfWork, IMapper mapper) : ITrackService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;

        // Implement methods from ITrackService here
        public async Task<TrackResponse> GetTrackResolverContext(ProjectionDefinition<Track> projection, string id)
        {
            Track track = await _unitOfWork.GetCollection<Track>()
                .Find(x => x.Id == id)
                .Project<Track>(projection)
                .FirstOrDefaultAsync();

            return _mapper.Map<TrackResponse>(track);
        }

        public async Task<IEnumerable<TrackResponse>> GetTracksAsync()
        {
            IEnumerable<Track> tracks = await _unitOfWork.GetCollection<Track>().Find(_ => true).ToListAsync();

            return _mapper.Map<IEnumerable<TrackResponse>>(tracks);
        }

        public async Task CreateTrackAsync(CreateTrackRequest trackRequest)
        {
            Track track = new()
            {
                Name = trackRequest.Name,
                Description = trackRequest.Description,
                ArtistId = trackRequest.ArtistId,
            };

            await _unitOfWork.GetCollection<Track>().InsertOneAsync(track);
        }
    }
}
