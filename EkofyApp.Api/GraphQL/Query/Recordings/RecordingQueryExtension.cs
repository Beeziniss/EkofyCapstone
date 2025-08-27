namespace EkofyApp.Api.GraphQL.Query.Recordings;

public sealed class RecordingQueryExtension : ObjectTypeExtension<RecordingQuery>
{
    protected override void Configure(IObjectTypeDescriptor<RecordingQuery> descriptor)
    {
        descriptor.Field(x => x.GetRecordingsQueryable())
            .Authorize(roles: "Moderator");

        descriptor.Field(x => x.GetMetadataRecordingUploadRequestAsync(default!))
            .Authorize(roles: "Moderator");
    }
}
