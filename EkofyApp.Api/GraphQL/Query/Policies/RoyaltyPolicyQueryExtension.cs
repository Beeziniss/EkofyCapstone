namespace EkofyApp.Api.GraphQL.Query.Policies;

public sealed class RoyaltyPolicyQueryExtension : ObjectTypeExtension<RoyaltyPolicyQuery>
{
    protected override void Configure(IObjectTypeDescriptor<RoyaltyPolicyQuery> descriptor)
    {
        descriptor.Field(x => x.GetRoyaltyPolicies())
            .Authorize(roles: "Admin");
        //.AllowAnonymous();
    }
}
