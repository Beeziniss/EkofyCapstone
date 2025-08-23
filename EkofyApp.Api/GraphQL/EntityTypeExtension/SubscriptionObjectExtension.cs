using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.EntityTypeExtension;
public sealed class SubscriptionObjectExtension : ObjectTypeExtension<Subscription>
{
    protected override void Configure(IObjectTypeDescriptor<Subscription> descriptor)
    {
    }
}
