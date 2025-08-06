using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{
    public interface IRiqsInterfaceEquipmentMigrationService
    {
        Task<string> ProcessUploadEquipmentData(UplaodEquipemtInterfaceDataCommand command);
        Task ProcessUploadEquipmentAdditionalData(UpdateEquipemtInterfaceAdditioanalDataCommand command);
        Task<RiqsEquipmentInterfaceMigrationSummary> GetEquipmentMigrationSummery(GetEquipmentInterfaceSummeryQuery query);
        Task<string> ProcessDownloadEquipmentData(DownloadEquipemtInterfaceDataCommand command);
    }
}
