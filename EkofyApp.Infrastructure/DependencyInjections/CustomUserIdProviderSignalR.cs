using Microsoft.AspNetCore.SignalR;

namespace EkofyApp.Infrastructure.DependencyInjections;

public class CustomUserIdProviderSignalR : IUserIdProvider
{
    public virtual string GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("userId")?.Value!;
    }
}
