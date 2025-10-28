using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.BillingConfigurations;

public sealed class BillingPortalConfigurationMutationExtension : ObjectTypeExtension<BillingPortalConfigurationMutation>
{
    protected override void Configure(IObjectTypeDescriptor<BillingPortalConfigurationMutation> descriptor)
    {
        descriptor.Field(x => x.CreateBillingPortalConfigurationAsync(default!))
            .Authorize(HelperRoleBase.AdminRolesArray);

        descriptor.Field(x => x.CreateCustomerPortalSessionAsync(default!, default))
            .Authorize(HelperRoleBase.ListenerArtistRolesArray);
    }
}
