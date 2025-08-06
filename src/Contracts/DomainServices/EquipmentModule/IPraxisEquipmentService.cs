using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisEquipmentService
    {
        Task<PraxisEquipment> GetPraxisEquipment(string itemId);
        void UpdatePraxisEquipment(string itemId);
        List<PraxisEquipment> GetAllPraxisPraxisEquipment();
        void AddRowLevelSecurity(string itemId, string clientId);
        void RemoveRowLevelSecurity(string clientId);
        Task<EntityQueryResponse<PraxisEquipment>> GetPraxisEquipments(string filter, string sort, int pageNumber, int pageSize);
        Task<EntityQueryResponse<PraxisEquipment>> GetEquipmentsReportData(string filter, string sort);
        Task GenerateQrFileForEquipment(PraxisEquipment equipment);
        Task UpdateRolesAllowedToReadOfPraxisEquipment();
        Task DeleteEquipmentFilesAsync(PraxisEquipment equipment);
        Task<PraxisGenericReportResult> PrepareEquipmentPhotoDocumentationData(GetReportQuery filter);
        Task AssignProcessGuide(AssignProcessGuideForEquipmentCommand command);
        Task UpdateEquipmentForAssignedProcessGuide(string equipmentId, string processGuideId);
        Task UpdateProcessGuideListingOfEquipment(AssignProcessGuideForEquipmentCommand command);
        Task DeleteProcessGuideFromEquipment(DeleteProcessGuideFromEquipmentCommand command);
        Task DeleteLibraryFilesFromEquipment(DeleteLibraryFilesFromEquipmentCommand command);
    }
}
