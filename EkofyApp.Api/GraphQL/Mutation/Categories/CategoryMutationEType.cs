namespace EkofyApp.Api.GraphQL.Mutation.Categories;

public class CategoryMutationEType : ObjectTypeExtension<CategoryMutation>
{
    protected override void Configure(IObjectTypeDescriptor<CategoryMutation> descriptor)
    {
        // You can define fields here if needed
        // For example:
        // descriptor.Field(x => x.CreateCategory(default)).Description("Creates a new category.");
        // descriptor.Field(x => x.UpdateCategory(default)).Description("Updates an existing category.");
        // descriptor.Field(x => x.DeleteCategory(default)).Description("Deletes a category by its ID.");
    }
}
