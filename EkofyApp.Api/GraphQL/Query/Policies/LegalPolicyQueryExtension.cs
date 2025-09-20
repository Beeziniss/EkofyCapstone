using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Policies;

public sealed class LegalPolicyQueryExtension : ObjectTypeExtension<LegalPolicyQuery>
{
    protected override void Configure(IObjectTypeDescriptor<LegalPolicyQuery> descriptor)
    {
        descriptor.Field(x => x.GetLegalPolicies())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting();
        //.AllowAnonymous();
    }
}
