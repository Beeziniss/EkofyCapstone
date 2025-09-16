namespace EkofyApp.Api.GraphQL.Mutation.Payment.Stripes;

public sealed class StripeMutationExtension : ObjectTypeExtension<StripeMutation>
{
    protected override void Configure(IObjectTypeDescriptor<StripeMutation> descriptor)
    {
        descriptor.Field(x => x.CreateExpressConnectedAccountAsync())
            .Authorize(roles: "Artist");

        //descriptor.Field(x => x.CreatePaymentCheckoutSessionAsync(default!))
        //    .Authorize(roles: "Listener"); // TODO: Sửa lại thêm role artist vì Listener,Artist lại bị lỗi

        descriptor.Field(x => x.CreateSubscriotionCheckoutSessionAsync(default!))
            .Authorize(roles: ["Listener", "Artist"]);
    }
}
