using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Categories;

public sealed class CategoryQueryExtension : ObjectTypeExtension<CategoryQuery>
{
    protected override void Configure(IObjectTypeDescriptor<CategoryQuery> descriptor)
    {
        descriptor.Field(x => x.GetCategories())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting<Category>();
        //.AllowAnonymous();
    }
}
