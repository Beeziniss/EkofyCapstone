using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.ServiceInterfaces.Tracks;

namespace EkofyApp.Api.GraphQL.Mutation.Tracks
{
    [ExtendObjectType(typeof(MutationInitialization))]
    [MutationType]
    public class TrackMutation(ITrackService trackService)
    {
        private readonly ITrackService _trackService = trackService;

        public async Task<bool> CreateTrackAsync(CreateTrackRequest trackRequest)
        {
            await _trackService.CreateTrackAsync(trackRequest);

            return true;
        }

        //public async Task<TrackDto> UpdateTrackAsync(UpdateTrackInput trackRequest)
        //{
        //    return await _trackService.UpdateTrackAsync(trackRequest);
        //}

        //public async Task<bool> DeleteTrackAsync(Guid id)
        //{
        //    return await _trackService.DeleteTrackAsync(id);
        //}
    }
}
