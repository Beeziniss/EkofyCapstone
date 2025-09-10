using EkofyApp.Api.GraphQL.Scalars;
using EkofyApp.Application.Models.Subscriptions;

namespace EkofyApp.Api.GraphQL.EntityTypeExtension;

public sealed class UpdateEntitlementInputType : InputObjectType<UpdateEntitlementRequest>
{
    protected override void Configure(IInputObjectTypeDescriptor<UpdateEntitlementRequest> descriptor)
    {
        descriptor.Field(x => x.Value)
            .Type<EntitlementValueScalar>();
    }
}
