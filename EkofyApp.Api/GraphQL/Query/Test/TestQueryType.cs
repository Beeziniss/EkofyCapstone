namespace EkofyApp.Api.GraphQL.Query.Test
{
    public class TestQueryType : ObjectTypeExtension<TestQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<TestQuery> descriptor)
        {
            // Configure the TestQuery type here if needed
            //descriptor.Name("TestQuery");
            //descriptor.Field(x => x.Test()).Description("Returns a test message.");
            //descriptor.Field(x => x.Test2()).Description("Returns another test message.");
        }
    }
}
