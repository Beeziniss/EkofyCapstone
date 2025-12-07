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

        [AllowAnonymous]
        public async Task<bool> SendFcmTokenAsync(string userId, string token)
        {
            return await _notificationService.SendFcmToken(userId, token);
        }


        #region For testing purpose only
        public async Task<bool> SendMultipleNotificationsAsync(IReadOnlyList<string> fcmTokens, string title, string body, string channelId)
        {
            await _notificationService.SendMultipleMessageAsync(fcmTokens, title, body, channelId);
            return true;
        }


        public async Task<bool> SendSingleNotificationAsync(string fcmTokens, string title, string body, string channelId, Dictionary<string, string>? data = null)
        {
            await _notificationService.SendFcmNotificationAsync(fcmTokens, title, body, channelId, data);
            return true;
        }
        #endregion
    }
}
