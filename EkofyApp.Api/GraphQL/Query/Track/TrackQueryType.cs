namespace EkofyApp.Api.GraphQL.Query.Track
{
    public class TrackQueryType : ObjectType<TrackQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<TrackQuery> descriptor)
        {
            //descriptor.Field(x => x.GetTrackById(default)).Description("Returns a track by its ID.");
            //descriptor.Field(x => x.GetAllTracks()).Description("Returns all tracks.");
        }
    }
}
