namespace EkofyApp.Api.GraphQL.Mutation.Payment.Stripes;

public sealed class StripeMutationExtension : ObjectTypeExtension<StripeMutation>
{
    protected override void Configure(IObjectTypeDescriptor<StripeMutation> descriptor)
    {
        // Configure the StripeMutation type here if needed
        descriptor.Field(x => x.CreateExpressConnectedAccountAsync())
            .Authorize(roles: "Artist");

        descriptor.Field(x => x.CreatePaymentCheckoutSessionAsync(default!))
            .Authorize(roles: "Listener,Artist");

        descriptor.Field(x => x.CreateSubscriotionCheckoutSessionAsync(default!))
            .Authorize(roles: "Listener,Artist");

        descriptor.Field(x => x.CreateSubscriptionPlanAsync(default!))
            .Authorize(roles: "Admin");
    }
}
