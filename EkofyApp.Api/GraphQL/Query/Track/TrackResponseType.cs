using EkofyApp.Application.Models.Track;

namespace EkofyApp.Api.GraphQL.Query.Track;

public class TrackResponseType : ObjectTypeExtension<TrackResponse>
{
    protected override void Configure(IObjectTypeDescriptor<TrackResponse> descriptor)
    {
        //descriptor.BindFieldsExplicitly();

        descriptor.Ignore(x => x.ArtistId);
    }
}
