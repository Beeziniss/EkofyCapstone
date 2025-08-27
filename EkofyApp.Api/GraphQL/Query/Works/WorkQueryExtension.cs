namespace EkofyApp.Api.GraphQL.Query.Works;

public sealed class WorkQueryExtension : ObjectTypeExtension<WorkQuery>
{
    protected override void Configure(IObjectTypeDescriptor<WorkQuery> descriptor)
    {
        descriptor.Field(x => x.GetWorksQueryable())
            .Authorize(roles: "Moderator");

        descriptor.Field(x => x.GetMetadataWorkUploadRequestAsync(default!))
            .Authorize(roles: "Moderator");

        // TODO: Artist cũng nên xem được các work của chính mình
    }
}
