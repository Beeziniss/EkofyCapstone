using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Entitlements;

public sealed class EntitlementQueryExtension : ObjectTypeExtension<EntitlementQuery>
{
    protected override void Configure(IObjectTypeDescriptor<EntitlementQuery> descriptor)
    {
        descriptor.Field(x => x.GetEntitlements())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting<Entitlement>();
        //.AllowAnonymous();
    }
}
