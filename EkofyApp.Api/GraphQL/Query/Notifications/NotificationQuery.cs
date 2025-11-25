using EkofyApp.Application.ServiceInterfaces.Notifications;
using EkofyApp.Domain.Entities;
using HotChocolate.Authorization;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Notifications;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class NotificationQuery(INotificationService notficationService)
{
    private readonly INotificationService _notficationService = notficationService;

    [AllowAnonymous]
    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Notification>]
    public IQueryable<Notification> GetNotifications()
    {
        return _notficationService.GetNotifications();
    }

    [AllowAnonymous]
    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Notification>]
    public IQueryable<Notification> GetNotificationsForUser(string userId)
    {
        return _notficationService.GetNotificationsForUser(userId);
    }

}
