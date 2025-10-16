using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.EntityTypeExtension;

public sealed class UserObjectExtension : ObjectTypeExtension<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Field(x => x.FCMToken).Ignore();
        descriptor.Field(x => x.PasswordHash).Ignore();
    }
}
