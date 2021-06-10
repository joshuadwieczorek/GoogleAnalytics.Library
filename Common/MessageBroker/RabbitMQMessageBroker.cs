using System;
using System.Text;
using RabbitMQ.Client;
using Newtonsoft.Json;
using AAG.Global.Common;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Configuration;
using GoogleAnalytics.Library.Helpers;

namespace GoogleAnalytics.Library.Common.MessageBroker
{
    public class RabbitMQMessageBroker : BaseActor<RabbitMQMessageBroker>, IMessageBroker
    {
        private RabbitMQConnectionFactory connectionFactory;
        private bool channelIsCreated = false;
        private string queueExchange;
        private string queueName;

        private IConnection connection
            => connectionFactory?.GetConnection();

        private IModel queueChannel
            => connection?.CreateModel();


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="bugSnag"></param>
        public RabbitMQMessageBroker(
              ILogger<RabbitMQMessageBroker> logger
            , Bugsnag.IClient bugSnag
            , IConfiguration configuration) : base(logger, bugSnag) 
        {
            connectionFactory = new RabbitMQConnectionFactory(
                configuration[StaticNames.MessageBrokerHost],
                configuration[StaticNames.MessageBrokerUser],
                configuration[StaticNames.MessageBrokerPassword]
            );
        }


        /// <summary>
        /// Initialize queue.
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="queueExchange"></param>
        public void Initialize(
              string queueName
            , string queueExchange = "")
        {
            queueChannel.QueueDeclare(queue: queueName,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: true,
                                 arguments: null);

            channelIsCreated = true;
            this.queueName = queueName;
            this.queueExchange = queueExchange;
        }


        /// <summary>
        /// Publish message on queue.
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="jsonSerialize"></param>
        public void PublishMessage<T>(
              T message
            , bool jsonSerialize = true)
        {
            if (channelIsCreated)
            {
                var messageBody = string.Empty;
                if (jsonSerialize)
                    messageBody = JsonConvert.SerializeObject(message);
                else
                    messageBody = message.ToString();

                var messageBytes = Encoding.UTF8.GetBytes(messageBody);

                queueChannel.BasicPublish(exchange: queueExchange,
                                 routingKey: queueName,
                                 basicProperties: null,
                                 body: messageBytes);
            }
            else
                throw new Exception("Channel is not created. Please initialize first!");
        }


        /// <summary>
        /// Start queue reader.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueItemProcessor"></param>
        public void StartQueueReader<T>(Action<T> queueItemProcessor)
        {
            if (channelIsCreated)
            {
                var consumer = new EventingBasicConsumer(queueChannel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    queueItemProcessor(JsonConvert.DeserializeObject<T>(message));
                };
                queueChannel.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);
            }
            else
                throw new Exception("Channel is not created. Please initialize first!");
        }
    }
}