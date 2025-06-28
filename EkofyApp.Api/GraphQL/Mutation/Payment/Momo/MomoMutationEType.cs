namespace EkofyApp.Api.GraphQL.Mutation.Payment.Momo;

public class MomoMutationEType : ObjectTypeExtension<MutationInitialization>
{
    protected override void Configure(IObjectTypeDescriptor<MutationInitialization> descriptor)
    {
        descriptor.Field("createMomoPaymentQR")
           .AllowAnonymous();
        descriptor.Field("createMomoPaymentVisa")
            .AllowAnonymous();
    }
}
