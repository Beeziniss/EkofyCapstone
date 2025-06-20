namespace EkofyApp.Api.GraphQL.Query.Test
{
    [ExtendObjectType(typeof(QueryInitialization))]
    public class TestQuery
    {
        public string Test() => "Test Query is ready!";
    }
}
