namespace EkofyApp.Domain.Settings.Redis;
public sealed record class RedisSetting
{
    public required string ConnectionStringSSL { get; init; }
    public required string ConnectionStringNoSSL { get; init; }

    public required string PublicEndpoint { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
}
