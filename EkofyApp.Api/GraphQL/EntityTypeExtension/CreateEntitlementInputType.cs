using EkofyApp.Api.GraphQL.Scalars;
using EkofyApp.Application.Models.Entitlements;

namespace EkofyApp.Api.GraphQL.EntityTypeExtension;
public sealed class CreateEntitlementInputType : InputObjectType<CreateEntitlementRequest>
{
    protected override void Configure(IInputObjectTypeDescriptor<CreateEntitlementRequest> descriptor)
    {
        descriptor.Field(x => x.DefaultValues)
            .Type<NonNullType<EntitlementValueScalar>>();
    }
}
