using EkofyApp.Application.Models.Tracks;

namespace EkofyApp.Api.GraphQL.Query.Tracks;

public class TrackResponseType : ObjectTypeExtension<TrackResponse>
{
    protected override void Configure(IObjectTypeDescriptor<TrackResponse> descriptor)
    {
        //descriptor.BindFieldsExplicitly();

        descriptor.Ignore(x => x.ArtistId);
    }
}
