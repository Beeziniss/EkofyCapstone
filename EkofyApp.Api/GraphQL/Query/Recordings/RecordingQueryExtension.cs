using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Recordings;

public sealed class RecordingQueryExtension : ObjectTypeExtension<RecordingQuery>
{
    protected override void Configure(IObjectTypeDescriptor<RecordingQuery> descriptor)
    {
        descriptor.Field(x => x.GetRecordingsQueryable())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting();

        descriptor.Field(x => x.GetMetadataRecordingUploadRequestAsync(default!))
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting();
    }
}
