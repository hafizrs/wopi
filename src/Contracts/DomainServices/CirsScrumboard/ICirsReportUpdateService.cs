using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;

public interface ICirsReportUpdateService
{
    Task InitiateUpdateAsync(AbstractUpdateCirsReportCommand command);
    Task UpdateLibraryFormResponse(ObjectArtifact artifact);
    Task UpdateFaultPermissions(string equipmentId, PraxisEquipmentRight equipmentRight);
}