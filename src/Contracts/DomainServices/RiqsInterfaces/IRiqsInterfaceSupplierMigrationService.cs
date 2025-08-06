using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{
    public interface IRiqsInterfaceSupplierMigrationService
    {
        Task<string> ProcessUploadSupplierData(UplaodSupplierInterfaceDataCommand command);
        Task ProcessUploadSupplierAdditionalData(UpdateSupplierInterfaceAdditioanalDataCommand command);
        Task<RiqsSupplierInterfaceMigrationSummary> GetSupplierInterfaceSummery(GetSupplierInterfaceSummeryQuery query);
        Task<string> ProcessDownloadSupplierData(DownloadSupplierInterfaceDataCommand command);
    }
}
