using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts
{
    public interface IBaseEventHandler<in T>
    {
        bool Handle(T data);
    }

    public interface IBaseEventHandlerAsync<T>
    {
        Task<bool> HandleAsync(T @event);
    }
}
