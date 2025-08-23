using EkofyApp.Api.GraphQL.Scalars;
using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Api.GraphQL.EntityTypeExtension;
public sealed class EntitlementObjectExtension : ObjectTypeExtension<Entitlement>
{
    protected override void Configure(IObjectTypeDescriptor<Entitlement> descriptor)
    {
        descriptor.Field(x => x.Value)
            .Type<NonNullType<EntitlementValueScalar>>();
    }
}
