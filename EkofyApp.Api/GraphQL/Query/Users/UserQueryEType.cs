using EkofyApp.Domain.Enums;

namespace EkofyApp.Api.GraphQL.Query.Users;

public class UserQueryEType : ObjectTypeExtension<UserQuery>
{
    protected override void Configure(IObjectTypeDescriptor<UserQuery> descriptor)
    {
        descriptor.Field(x => x.GetUsers())
            //.UsePaging<User>(options =>
            //{
            //    options.ProviderName = PagingProviderNames.Cursor; // CURSOR PAGING
            //    options.IncludeTotalCount = true;
            //})
            .UseProjection()
            .UseFiltering()
            .UseSorting();

        descriptor.Field(x => x.GetUserByIdAsync(default!))
        .Authorize([UserRole.Admin.ToString()]);
    }
}
