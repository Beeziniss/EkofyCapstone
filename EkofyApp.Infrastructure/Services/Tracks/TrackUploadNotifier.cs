using EkofyApp.Application.ServiceInterfaces.Tracks;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EkofyApp.Infrastructure.Services.Tracks;

public sealed class TrackUploadNotifier(IHubContext<TrackUploadHub> hubContext) : ITrackUploadNotifier
{
    private readonly IHubContext<TrackUploadHub> _hubContext = hubContext;

    public Task SendProgressAsync(string userId, int percent, string stepDescription)
    {
        return _hubContext.Clients.User(userId)
        .SendAsync("ReceiveProgress", new { percent, stepDescription });
    }

    public Task SendCompletedAsync(string userId)
    {
        return _hubContext.Clients.User(userId).SendAsync("ReceiveCompleted");
    }

    public Task SendFailedAsync(string userId, string errorMessage)
    {
        return _hubContext.Clients.User(userId)
        .SendAsync("ReceiveFailed", errorMessage);
    }
}
