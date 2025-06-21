using AutoMapper;
using EkofyApp.Application.Models.Track;
using EkofyApp.Application.ServiceInterfaces.Track;
using EkofyApp.Domain.Entities;
using HealthyNutritionApp.Application.Interfaces;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Track
{
    public class TrackService(IUnitOfWork unitOfWork, IMapper mapper) : ITrackService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;

        // Implement methods from ITrackService here
        public async Task<TrackResponse> GetTrackResolverContext(ProjectionDefinition<Tracks> projection, string id)
        {
            Tracks track = await _unitOfWork.GetCollection<Tracks>()
                .Find(x => x.Id == id)
                .Project<Tracks>(projection)
                .FirstOrDefaultAsync();

            return _mapper.Map<TrackResponse>(track);
        }

        public async Task<IEnumerable<TrackResponse>> GetTracksAsync()
        {
            IEnumerable<Tracks> tracks = await _unitOfWork.GetCollection<Tracks>().Find(_ => true).ToListAsync();

            return _mapper.Map<IEnumerable<TrackResponse>>(tracks);
        }

        public async Task CreateTrackAsync(CreateTrackRequest trackRequest)
        {
            Tracks track = new()
            {
                Name = trackRequest.Name,
                Description = trackRequest.Description,
                ArtistId = trackRequest.ArtistId,
            };

            await _unitOfWork.GetCollection<Tracks>().InsertOneAsync(track);
        }
    }
}
