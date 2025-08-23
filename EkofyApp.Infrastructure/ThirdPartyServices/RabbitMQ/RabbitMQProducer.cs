using EkofyApp.Application.ThirdPartyServiceInterfaces.RabbitMQ;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace EkofyApp.Infrastructure.ThirdPartyServices.RabbitMQ
{
    public class RabbitMQProducer : IMessageProducer
    {
        private readonly IRabbitMQConnection _connection;

        public RabbitMQProducer(IRabbitMQConnection connection)
        {
            _connection = connection;
        }

        public void SendMessage<T>(T message, string queueName)
        {
            using var channel = _connection.Connection.CreateModel();

            //khai báo 1 queue , nếu chưa có thì tạo mới
            channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            //chuyển cái object message sang dạng byte để gửi, rabbitMQ chỉ nhận byte
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            //gửi mesage dã dc byte hóa vào queue cụ thể đã có ở trên
            channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
        }
    }
}
