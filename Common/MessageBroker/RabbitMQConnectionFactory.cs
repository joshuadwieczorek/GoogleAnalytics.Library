using AAG.Global.ExtensionMethods;
using RabbitMQ.Client;

namespace GoogleAnalytics.Library.Common.MessageBroker
{
    public class RabbitMQConnectionFactory
    {        
        private string _rabbitMQHost;
        private string _rabbitMQUser;
        private string _rabbitMQPassword;

        private string rabbitMQHost
        {
            get
            {
                if (!_rabbitMQHost.HasValue())
                    _rabbitMQHost = "localhost";

                return _rabbitMQHost;
            }
            set => _rabbitMQHost = value;
        }

        /// <summary>
        /// RabbitMQ connection.
        /// </summary>
        private IConnection _connection;
        private IConnection connection
        {
            get
            {
                if (_connection is null)
                    _connection = ConnectionFactory()
                        .CreateConnection();

                return _connection;
            }
        }        


        /// <summary>
        /// Generate a new connection factory.
        /// </summary>
        /// <returns></returns>
        private ConnectionFactory ConnectionFactory()
            => new ConnectionFactory() 
            { 
                HostName = rabbitMQHost,
                UserName = _rabbitMQUser,
                Password = _rabbitMQPassword
            };


        /// <summary>
        /// Initalize connection factory.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        internal RabbitMQConnectionFactory(
              string host
            , string user = "guest"
            , string password = "guest")
        {
            _rabbitMQHost = host;
            _rabbitMQUser = user;
            _rabbitMQPassword = password;
        }


        /// <summary>
        /// Get RabbitMQ connection.
        /// </summary>
        /// <returns></returns>
        internal IConnection GetConnection()
            => connection;
    }
}