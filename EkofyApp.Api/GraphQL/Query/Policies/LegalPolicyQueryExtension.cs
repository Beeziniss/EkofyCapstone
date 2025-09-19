namespace EkofyApp.Api.GraphQL.Query.Policies;

public sealed class LegalPolicyQueryExtension : ObjectTypeExtension<LegalPolicyQuery>
{
    protected override void Configure(IObjectTypeDescriptor<LegalPolicyQuery> descriptor)
    {
        descriptor.Field(x => x.GetLegalPolicies())
            .Authorize(roles: ["Listener", "Artist", "Moderator", "Admin"]);
        //.AllowAnonymous();
    }
}
