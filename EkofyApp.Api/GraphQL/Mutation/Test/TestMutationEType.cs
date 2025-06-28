namespace EkofyApp.Api.GraphQL.Mutation.Test
{
    public class TestMutationEType : ObjectTypeExtension<TestMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<TestMutation> descriptor)
        {
            descriptor.Field(x => x.UploadFileAsync(default!, default!, default!))
                .AllowAnonymous();

            descriptor.Field(x => x.ConvertToWavFileAsync(default!, default!, default!))
                .AllowAnonymous();

            descriptor.Field(x => x.ConvertToHlsAsync(default!, default!, default!))
                .AllowAnonymous();
        }
    }
}
