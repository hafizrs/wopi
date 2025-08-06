using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{
    public interface IRiqsInterfaceUserMigrationService
    {
        Task<string> ProcessUploadUserData(UplaodUserInterfaceDataCommand command);
        Task ProcessUploadUserAdditionalData(UpdateUserInterfaceAdditioanalDataCommand command);
        Task<RiqsUserInterfaceMigrationSummary> GetUserMigrationSummery(GetUserInterfaceSummeryQuery query);
        Task<string> ProcessDownloadUserData(DownloadUserInterfaceDataCommand command);
    }
}
