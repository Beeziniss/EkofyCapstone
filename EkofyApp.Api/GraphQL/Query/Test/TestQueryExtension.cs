namespace EkofyApp.Api.GraphQL.Query.Test;

public sealed class TestQueryExtension : ObjectTypeExtension<TestQuery>
{
    protected override void Configure(IObjectTypeDescriptor<TestQuery> descriptor)
    {
        descriptor.Field(x => x.GetEntitlements(default!))
            .AllowAnonymous();
    }
}
