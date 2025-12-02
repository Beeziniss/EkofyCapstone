using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Policies;

public sealed class EscrowComissionPolicyMutationExtention : ObjectTypeExtension<EscrowComissionPolicyMutation>
{
    protected override void Configure(IObjectTypeDescriptor<EscrowComissionPolicyMutation> descriptor)
    {
        descriptor.Field(x => x.SeedEscrowCommissionPolicyDataAsync(default!))
            .AllowAnonymous();

        descriptor.Field(x => x.CreateEscrowCommissionPolicyAsync(default!))
            .Authorize(HelperRoleBase.AdminRolesArray);

        descriptor.Field(x => x.UpdateEscrowCommissionPolicyAsync(default!))
            .Authorize(HelperRoleBase.AdminRolesArray);

        descriptor.Field(x => x.DowngradeEscrowCommissionPolicyVersionAsync(default))
            .Authorize(HelperRoleBase.AdminRolesArray);

        descriptor.Field(x => x.SwitchEscrowCommissionPolicyToLatestVersionAsync())
            .Authorize(HelperRoleBase.AdminRolesArray);
    }
}
