using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Users;

public sealed class UserEType : ObjectTypeExtension<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Field(x => x.FCMToken).Ignore();
        descriptor.Field(x => x.PasswordHash).Ignore();
        descriptor.Field(x => x.RefreshToken).Ignore();
        descriptor.Field(x => x.RefreshTokenExpiryTime).Ignore();
    }
}
