namespace EkofyApp.Api.GraphQL.Mutation.Payment.Momo;

public class MomoMutationExtension : ObjectTypeExtension<MutationInitialization>
{
    protected override void Configure(IObjectTypeDescriptor<MutationInitialization> descriptor)
    {
        descriptor.Field("createMomoPaymentQR")
           .AllowAnonymous();
        descriptor.Field("createMomoPaymentVisa")
            .AllowAnonymous();
    }
}
