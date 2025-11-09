using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Revenues;

public sealed class ArtistRevenueMutationExtension : ObjectTypeExtension<ArtistRevenueMutation>
{
    protected override void Configure(IObjectTypeDescriptor<ArtistRevenueMutation> descriptor)
    {
        descriptor.Field(x => x.ComputeArtistRevenueByArtistIdAsync(default!))
            .AllowAnonymous();
    }
}
