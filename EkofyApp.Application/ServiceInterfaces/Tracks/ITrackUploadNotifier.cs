
namespace EkofyApp.Application.ServiceInterfaces.Tracks;

public interface ITrackUploadNotifier
{
    Task SendCompletedAsync(string userId);
    Task SendFailedAsync(string userId, string errorMessage);
    Task SendProgressAsync(string userId, int percent, string stepDescription);
}
