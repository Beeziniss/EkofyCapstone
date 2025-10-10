using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Users;

public sealed class UserMutationExtension : ObjectTypeExtension<UserMutation>
{
    protected override void Configure(IObjectTypeDescriptor<UserMutation> descriptor)
    {
        // Configure the UserMutation type here if needed
        descriptor.Field(x => x.CreateModeratorAsync(default!))
            .Authorize(HelperRoleBase.AdminRolesArray);

        descriptor.Field(x => x.CreateAdminAsync(default!))
            //.AllowAnonymous();
        .Authorize(HelperRoleBase.AdminRolesArray);

        descriptor.Field(x => x.BanUserAsync(default!))
            .Authorize(HelperRoleBase.ModeratorAdminRolesArray);

        descriptor.Field(x => x.ReActiveUserAsync(default!))
            .Authorize(HelperRoleBase.ModeratorAdminRolesArray);
    }
}
