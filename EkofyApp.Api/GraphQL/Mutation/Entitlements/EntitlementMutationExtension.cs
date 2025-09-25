namespace EkofyApp.Api.GraphQL.Mutation.Entitlements;

public sealed class EntitlementMutationExtension : ObjectTypeExtension<EntitlementMutation>
{
    protected override void Configure(IObjectTypeDescriptor<EntitlementMutation> descriptor)
    {
        descriptor.Field(x => x.SeedEntitlementsAsync(default!))
            .Authorize(roles: "Admin");
            //.AllowAnonymous();

        descriptor.Field(x => x.CreateEntitlementAsync(default!))
            .Authorize(roles: "Admin");

        descriptor.Field(x => x.GetEntitlementUserCountAsync(default!))
            .Authorize(roles: "Admin");

        descriptor.Field(x => x.DeactiveEntitlementAsync(default!))
            .Authorize(roles: "Admin");

        descriptor.Field(x => x.ReactiveEntitlementAsync(default!))
            .Authorize(roles: "Admin");
    }
}
