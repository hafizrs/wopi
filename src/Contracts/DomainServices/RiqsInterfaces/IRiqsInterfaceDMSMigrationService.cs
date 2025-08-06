using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.DmsMigration;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsInterfaces;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{
    public interface IRiqsInterfaceDMSMigrationService
    {
        Task InitiateInterfaceMigration(InterfaceMigrationFolderAndFileCommand command);
        Task CreateInterfaceMigrationFolder(RiqsInterfaceMigrationSummary migrationSummery, Workspace workspace);
        Task UploadInterfaceMigrationFile(RiqsInterfaceMigrationSummary migrationSummery, Workspace workspace);
    }
}
