namespace EkofyApp.Api.GraphQL.Mutation.Policies;

public sealed class RoyaltyPolicyMutationExtension : ObjectTypeExtension<RoyaltyPolicyMutation>
{
    protected override void Configure(IObjectTypeDescriptor<RoyaltyPolicyMutation> descriptor)
    {
        descriptor.Field(x => x.SeedRoyaltyPolicyDataAsync(default!))
            .AllowAnonymous();

        descriptor.Field(x => x.CreateRoyaltyPolicyAsync(default!))
            .Authorize(roles: "Admin");
    }
}
