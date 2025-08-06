using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.WopiMonitor.Contracts.Infrastructure
{
    public interface IServiceClient
    {
        T SendToQueue<T>(string queueName, object message);
    }
} 