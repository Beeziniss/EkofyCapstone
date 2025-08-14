using EkofyApp.Api.GraphQL.Scalars;
using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Api.GraphQL.Query.Test;

public sealed class TestEType : ObjectTypeExtension<Entitlement>
{
    protected override void Configure(IObjectTypeDescriptor<Entitlement> descriptor)
    {
        descriptor.Field(x => x.Value)
            .Type<NonNullType<EntitlementValueScalar>>();
    }
}
