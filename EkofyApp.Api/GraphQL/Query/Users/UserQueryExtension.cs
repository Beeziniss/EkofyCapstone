using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Users;

public class UserQueryExtension : ObjectTypeExtension<UserQuery>
{
    protected override void Configure(IObjectTypeDescriptor<UserQuery> descriptor)
    {
        descriptor.Field(x => x.GetUsers())
            //.UsePaging<User>(options =>
            //{
            //    options.ProviderName = PagingProviderNames.Cursor; // CURSOR PAGING
            //    options.IncludeTotalCount = true;
            //})
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting<User>();

        descriptor.Field(x => x.GetUserByIdAsync(default!))
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting<User>();
    }
}
