using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface ISchedulerService
    {
        Task<bool> ProcesNotFullfilledTask();
        Task<bool> SendMaintenanceMailToResposibleUser();
        
    }
}
