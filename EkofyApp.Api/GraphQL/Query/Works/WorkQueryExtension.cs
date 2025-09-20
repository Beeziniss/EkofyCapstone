using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Works;

public sealed class WorkQueryExtension : ObjectTypeExtension<WorkQuery>
{
    protected override void Configure(IObjectTypeDescriptor<WorkQuery> descriptor)
    {
        descriptor.Field(x => x.GetWorksQueryable())
            .Authorize(roles: HelperRoleBase.ModeratorRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting();

        descriptor.Field(x => x.GetMetadataWorkUploadRequestAsync(default!))
            .Authorize(roles: HelperRoleBase.ModeratorRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting();

        // TODO: Artist cũng nên xem được các work của chính mình
    }
}
