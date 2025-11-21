using EkofyApp.Application.Models.TopTracks;
using EkofyApp.Application.ServiceInterfaces.TopTracks;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.TopTracks
{
    [ExtendObjectType(typeof(QueryInitialization))]
    [QueryType]
    public class TopTrackQuery(ITopTrackService topTrackService)
    {
        private readonly ITopTrackService _topTrackService = topTrackService;

        [AuthorizeRoles(HelperRoleBase.ListenerRoles)]
        [UseProjection]
        public IQueryable<TopTrackResponse> GetTopTracks()
        {
            return _topTrackService.GetOwnTopTracks();
        }
    }
}
