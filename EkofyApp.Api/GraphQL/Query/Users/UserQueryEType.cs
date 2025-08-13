namespace EkofyApp.Api.GraphQL.Query.Users;

public sealed class UserQueryEType : ObjectTypeExtension<UserQuery>
{
    protected override void Configure(IObjectTypeDescriptor<UserQuery> descriptor)
    {
        descriptor.Field(x => x.GetUsers())
            .UseProjection()
            .UseFiltering()
            .UseSorting();
    }
}
