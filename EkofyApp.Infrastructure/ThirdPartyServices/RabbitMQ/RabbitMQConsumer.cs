using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SoundFingerprinting.Configuration;
using System.Text;

namespace EkofyApp.Infrastructure.ThirdPartyServices.RabbitMQ
{
    public class RabbitMQConsumer : BackgroundService
    {
        
        private IConnection? _connection;
        private IModel? _channel;

        /*
        public RabbitMQConsumer()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                // địa chỉ gửi message
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }
        public void StartConsuming(string queueName)
        {
            //khai báo 1 queue , đảm bảo tồn tại
            _channel.QueueDeclare(queue: queueName, exclusive: false);

            //tạo consumer để lắng nghe message từ queue, mỗi khi có message thì gọi event Received
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (sender, args) =>
            {
                var body = args.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                // chỗ này chỉ mới lấy mesage ra dạng json, chưa map về object model
                // var messageObject = JsonSerializer.Deserialize<Model>(json);
                //dòng trên để map về object model (tham khảo)
            };

            //consume message từ queue
            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }
        */

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                // địa chỉ gửi message
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            //chỗ này đang fix cứng queue name để tránh lỗi, sau lấy từ biến môi trường
            _channel.QueueDeclare(queue: "queue-name", durable: true, exclusive: false, autoDelete: false);

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel!);

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                // chỗ này chỉ mới lấy mesage ra dạng json, chưa map về object model
                // var messageObject = JsonSerializer.Deserialize<Model>(json);
                //dòng trên để map về object model (tham khảo)
            };

            //chỗ này đang fix cứng queue name để tránh lỗi, sau lấy từ biến môi trường
            _channel!.BasicConsume(queue: "queue-name", autoAck: true, consumer: consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
