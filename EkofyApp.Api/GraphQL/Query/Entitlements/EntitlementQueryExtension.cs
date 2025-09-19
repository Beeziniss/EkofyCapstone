namespace EkofyApp.Api.GraphQL.Query.Entitlements;

public sealed class EntitlementQueryExtension : ObjectTypeExtension<EntitlementQuery>
{
    protected override void Configure(IObjectTypeDescriptor<EntitlementQuery> descriptor)
    {
        descriptor.Field(x => x.GetEntitlements())
            .Authorize(roles: "Admin");
        //.AllowAnonymous();
    }
}
