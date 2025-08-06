using Selise.Ecap.SC.WopiMonitor.Contracts.Infrastructure;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.WopiMonitor.Utils
{
    public class ServiceClient : IServiceClient
    {
        public T SendToQueue<T>(string queueName, object message)
        {
            // Implementation for sending to queue
            // This would typically integrate with a message queue system
            return default(T);
        }
    }
} 