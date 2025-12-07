using EkofyApp.Application.ServiceInterfaces.Notifications;
using EkofyApp.Domain.Utils;
using HotChocolate.Authorization;

namespace EkofyApp.Api.GraphQL.Mutation.Notifications
{
    [ExtendObjectType<MutationInitialization>]
    [MutationType]
    public sealed class NotificationMutation (INotificationService notificationService)
    {

        private readonly INotificationService _notificationService = notificationService;

        //[AllowAnonymous]
        //public async Task<bool> SendFcmTokenAsync(string userId, string token)
        //{
        //    return await _notificationService.SendFcmToken(userId, token);
        //}


        #region For testing purpose only
        public async Task<bool> SendSingleNotificationAsync(string? userId, string title, string body, string channelId, Dictionary<string, string>? data = null)
        {
            await _notificationService.SendFcmNotificationAsync(userId, title, body, channelId, data);
            return true;
        }
        #endregion
    }
}
