using EkofyApp.Api.GraphQL.Mutation.PackageOrders;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Notifications
{
    public class NotificationMutationExtension : ObjectTypeExtension<NotificationMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<NotificationMutation> descriptor)
        {
            descriptor.Field(x => x.SendMultipleNotificationsAsync(default!, default!, default!, default!))
                .Authorize(HelperRoleBase.ListenerRolesArray);
            descriptor.Field(x => x.SendSingleNotificationAsync(default!, default!, default!, default!, default))
                .Authorize(HelperRoleBase.ListenerRolesArray);
        }
    }
}
