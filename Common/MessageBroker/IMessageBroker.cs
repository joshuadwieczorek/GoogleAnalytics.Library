using System;

namespace GoogleAnalytics.Library.Common.MessageBroker
{
    public interface IMessageBroker
    {
        void Initialize(string queueName, string queueExchange = "");
        void PublishMessage<T>(T message, bool jsonSerialize);
        void StartQueueReader<T>(Action<T> queueItemProcessor);
    }
}