using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Mutation.BillingConfigurations;

public sealed class BillingPortalConfigurationMutationExtension : ObjectTypeExtension<BillingPortalConfigurationMutation>
{
    protected override void Configure(IObjectTypeDescriptor<BillingPortalConfigurationMutation> descriptor)
    {
        descriptor.Field(x => x.CreateBillingPortalConfigurationAsync(default!))
            .Authorize(roles: "Admin");

        descriptor.Field(x => x.CreateCustomerPortalSessionAsync(default!, default))
            .Authorize(roles: "Listener,Artist");
    }
}
