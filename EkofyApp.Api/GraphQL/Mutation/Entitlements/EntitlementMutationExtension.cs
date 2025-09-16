namespace EkofyApp.Api.GraphQL.Mutation.Entitlements;

public sealed class EntitlementMutationExtension : ObjectTypeExtension<EntitlementMutation>
{
    protected override void Configure(IObjectTypeDescriptor<EntitlementMutation> descriptor)
    {
        descriptor.Field(x => x.GetEntitlements())
            .Authorize(roles: "Admin");

        descriptor.Field(x => x.SeedEntitlementsAsync())
            .Authorize(roles: "Admin");
    }
}
