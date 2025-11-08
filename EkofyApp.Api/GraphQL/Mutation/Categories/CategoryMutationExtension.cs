namespace EkofyApp.Api.GraphQL.Mutation.Categories;

public class CategoryMutationExtension : ObjectTypeExtension<CategoryMutation>
{
    protected override void Configure(IObjectTypeDescriptor<CategoryMutation> descriptor)
    {
        // You can define fields here if needed
        // For example:
        // descriptor.Field(x => x.CreateCategory(default)).PackageDescription("Creates a new category.");
        // descriptor.Field(x => x.UpdateCategory(default)).PackageDescription("Updates an existing category.");
        // descriptor.Field(x => x.DeleteCategory(default)).PackageDescription("Deletes a category by its ID.");
    }
}
