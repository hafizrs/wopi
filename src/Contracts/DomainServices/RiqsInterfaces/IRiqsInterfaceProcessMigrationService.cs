using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{

    public interface IRiqsInterfaceProcessMigrationService
    {
        Task<bool> InitiateProcessFileMigration(ProcessInterfaceMigrationCommand command);
    }
}
