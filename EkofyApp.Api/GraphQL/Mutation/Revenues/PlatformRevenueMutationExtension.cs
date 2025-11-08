namespace EkofyApp.Api.GraphQL.Mutation.Revenues;

public sealed class PlatformRevenueMutationExtension : ObjectTypeExtension<PlatformRevenueMutation>
{
    protected override void Configure(IObjectTypeDescriptor<PlatformRevenueMutation> descriptor)
    {
        descriptor.Field(x => x.ComputePlatformRevenueAsync())
            .AllowAnonymous();
    }
}
