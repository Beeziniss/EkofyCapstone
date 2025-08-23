using RabbitMQ.Client;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.RabbitMQ
{
    public interface IRabbitMQConnection
    {
        IConnection Connection { get; }
    }
}
