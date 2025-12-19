using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Tracks;

public sealed class FingerprintConfidenceMutationExtension : ObjectTypeExtension<FingerprintConfidenceMutation>
{
    protected override void Configure(IObjectTypeDescriptor<FingerprintConfidenceMutation> descriptor)
    {
        descriptor.Field(x => x.UpdateFingerprintConfidencePolicyAsync(default!))
            .Authorize(HelperRoleBase.AdminRolesArray);
    }
}
