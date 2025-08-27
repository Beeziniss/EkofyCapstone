namespace EkofyApp.Api.GraphQL.Mutation.Users;

public sealed class UserMutationExtension : ObjectTypeExtension<UserMutation>
{
    protected override void Configure(IObjectTypeDescriptor<UserMutation> descriptor)
    {
        // Configure the UserMutation type here if needed
        descriptor.Field(x => x.CreateModeratorAsync(default!))
            .Authorize(roles: "Admin");

        descriptor.Field(x => x.CreateAdminAsync(default!))
            //.AllowAnonymous();
        .Authorize(roles: "Admin");
    }
}
