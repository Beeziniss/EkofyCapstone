using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Payment.Stripes;

public sealed class StripeMutationExtension : ObjectTypeExtension<StripeMutation>
{
    protected override void Configure(IObjectTypeDescriptor<StripeMutation> descriptor)
    {
        descriptor.Field(x => x.CreateExpressConnectedAccountAsync(default!, default!))
            .Authorize(roles: HelperRoleBase.ArtistRolesArray);

        descriptor.Field(x => x.CreatePaymentCheckoutSessionAsync(default!))
            .Authorize(roles: HelperRoleBase.ListenerArtistRolesArray);

        descriptor.Field(x => x.CreateSubscriptionCheckoutSessionAsync(default!))
            .Authorize(roles: HelperRoleBase.ListenerArtistRolesArray);

        descriptor.Field(x => x.CancelSubscriptionAtPeriodEndAsync())
            .Authorize(roles: HelperRoleBase.ListenerArtistRolesArray);

        descriptor.Field(x => x.ResumeSubscriptionAsync())
            .Authorize(roles: HelperRoleBase.ListenerArtistRolesArray);
    }
}
