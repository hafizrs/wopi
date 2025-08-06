using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface ITasksUpdateService
    {
        Task<bool> UpdateTasks(TasksUpdateCommand command);
    }
}