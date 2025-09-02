namespace EkofyApp.Api.GraphQL.Query.Payment.Stripes;

public sealed class StripeQueryExtension : ObjectTypeExtension<StripeQuery>
{
    protected override void Configure(IObjectTypeDescriptor<StripeQuery> descriptor)
    {
        //descriptor.Field(x => x.GetBalance())
        //    .Authorize(roles: "Admin");
            //.AllowAnonymous();

        descriptor.Field(x => x.CreateCustomerPortalSessionAsync(default!))
            .Authorize(roles: "Listener,Artist");
    }
}
