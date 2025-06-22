namespace EkofyApp.Api.GraphQL.Mutation.Test
{
    public class TestMutationType : ObjectTypeExtension<TestMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<TestMutation> descriptor)
        {
            // Configure the TestMutation type here if needed
            // For example, you can define fields or descriptions for the mutation
            // descriptor.Name("TestMutation");
            // descriptor.Field(x => x.SomeMutationMethod()).Description("Description of the mutation method.");
        }
    }
}
