using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{

    public interface IRiqsInterfaceGoogleDriveMigrationService
    {
        Task<bool> ProcessFileMigration(ProcessInterfaceMigrationCommand command, ExternalUserTokenResponse tokenInfo);
    }
}
