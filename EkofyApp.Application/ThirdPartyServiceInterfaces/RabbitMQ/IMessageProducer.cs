namespace EkofyApp.Application.ThirdPartyServiceInterfaces.RabbitMQ
{
    public interface IMessageProducer
    {
        void SendMessage<T> (T message, string queueName);
    }
}
