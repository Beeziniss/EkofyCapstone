namespace EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
public interface ICacheResult<out T>
{
    bool Success { get; }
    T? Value { get; }
    TimeSpan? TimeToLive { get; }
}