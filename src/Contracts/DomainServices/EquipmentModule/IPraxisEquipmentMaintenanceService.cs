using System.Collections.Generic;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisEquipmentMaintenanceService
    {
        PraxisEquipmentMaintenance GetPraxisEquipmentMaintenance(string itemId);
        void UpdatePraxisEquipmentMaintenance(string itemId);
        List<PraxisEquipmentMaintenance> GetAllPraxisEquipmentMaintenance();
        void AddRowLevelSecurity(string itemId, string clientId);
        void RemoveRowLevelSecurity(string clientId);
        Task<EntityQueryResponse<PraxisEquipmentMaintenance>> GetPraxisMaintenances(string filter, string sort, int pageNumber, int pageSize);
        Task UpdateEquipmentMaintenanceLibraryFormResponse(ObjectArtifact artifact);
        Task<Dictionary<string, object>> GetEquipmentMaintenanceForExternalUser(GetEquipmentMaintenanceForExternalUserQuery query);
        Task<Dictionary<string, object>> GetEquipmentForExternalUser(GetEquipmentForExternalUserQuery query);
        Task ProcessScheduledMaintenance(ProcessScheduledMaintenance command, CommandResponse response);
        Task CreateMaintenance(CreateMaintenanceCommand command);
        Task AssignTasks(string maintenanceId, bool assignTask);
        Task UpdateMaintenanceForProcessGuideCreated(string maintenanceId, string processGuideId);
        void DeleteDependentEntitiesForEquipmentMaintenance(List<string> maintenanceIds);
        Task<PraxisGenericReportResult> PrepareEquipmentMaintenancePhotoDocumentationData(GetReportQuery filter);
        Task<bool> ProcessEmailForResponsibleUsers(PraxisEquipmentMaintenance equipmentMaintenance);
        void SendMailOnEquipmentMaintenanceDelete(PraxisEquipmentMaintenance equipmentMaintenance, string equipmentName);
        Task<PraxisEquipmentMaintenanceSupplierInfo> GetEquipmentMaintenanceSupplierInfo(string equipementId , string supplierId);
        Task UpdatePraxisEquipmentMaintenanceDates(string maintenanceItemId, PraxisEquipmentMaintenance maintenance);
        void UpdatePraxisEquipmentMaintenanceDatesMetaData(PraxisEquipment praxisEquipment, MaintenanceDatePropWithType toUpdateMaintenanceWithType);
    }
}
