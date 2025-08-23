using EkofyApp.Application.ThirdPartyServiceInterfaces.RabbitMQ;
using RabbitMQ.Client;

namespace EkofyApp.Infrastructure.ThirdPartyServices.RabbitMQ
{
    public class RabbitMQConnection : IRabbitMQConnection, IDisposable
    {
        private IConnection? _connection;
        public IConnection Connection => _connection!;

        public RabbitMQConnection()
        {
            InitializeConnection();
        }

        private void InitializeConnection()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                // nếu có username và password thì thêm vào, tạm thời chưa
            };
            _connection = factory.CreateConnection();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
