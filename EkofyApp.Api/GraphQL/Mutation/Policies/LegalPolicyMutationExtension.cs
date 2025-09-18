namespace EkofyApp.Api.GraphQL.Mutation.Policies;

public sealed class LegalPolicyMutationExtension : ObjectTypeExtension<LegalPolicyMutation>
{
    protected override void Configure(IObjectTypeDescriptor<LegalPolicyMutation> descriptor)
    {
        descriptor.Field(x => x.CreateLegalPolicyAsync(default!))
            .Authorize(roles: "Admin");
    }
}
