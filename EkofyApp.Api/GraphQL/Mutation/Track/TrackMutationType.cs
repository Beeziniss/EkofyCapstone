namespace EkofyApp.Api.GraphQL.Mutation.Track
{
    public class TrackMutationType : ObjectType<TrackMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<TrackMutation> descriptor)
        {
            // Configure the TrackMutation type here if needed
            //descriptor.Name("TrackMutation");
            //descriptor.Field(x => x.CreateTrack(default)).Description("Creates a new track.");
            //descriptor.Field(x => x.UpdateTrack(default)).Description("Updates an existing track.");
            //descriptor.Field(x => x.DeleteTrack(default)).Description("Deletes a track by its ID.");
            //descriptor.Field(x => x.GetTrackById(default)).Description("Returns a track by its ID.");
            //descriptor.Field(x => x.GetAllTracks()).Description("Returns all tracks.");
        }
    }
}
