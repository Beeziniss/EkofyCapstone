namespace EkofyApp.Api.GraphQL.Query.Requests
{
    public class RequestQueryExtension
    {
        protected void Configure(IObjectTypeDescriptor<RequestQuery> descriptor)
        {
            descriptor.Field(x => x.GetRequests())
                .UseProjection()
                .UseFiltering()
                .UseSorting();

        }

    }
}
