using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataForEquipmentMaintenance : IDeleteDataByCollectionSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteDataForEquipmentMaintenance> _logger;
        private readonly IPraxisEquipmentMaintenanceService _praxisEquipmentMaintenanceService;
        private readonly IGenericEventPublishService _genericEventPublishService;

        public DeleteDataForEquipmentMaintenance(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteDataForEquipmentMaintenance> logger,
            IPraxisEquipmentMaintenanceService praxisEquipmentMaintenanceService,
            IGenericEventPublishService genericEventPublishService
        )
        {
            _securityContextProviderService = securityContextProviderService;
            _repository = repository;
            _logger = logger;
            _praxisEquipmentMaintenanceService = praxisEquipmentMaintenanceService;
            _genericEventPublishService = genericEventPublishService;
        }
        public async Task<bool> DeleteData(string entityName, string itemId, string additionalInfosItemId = null, string additionalTitleId = null)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation("Going to delete {EntityName} data with ItemId: {ItemId} and tenantId: {TenantId}.", nameof(PraxisEquipmentMaintenance), itemId, securityContext.TenantId);
            try
            {
                var existingMaintainance = await _repository.GetItemAsync<PraxisEquipmentMaintenance>(m => m.ItemId == itemId && !m.IsMarkedToDelete);
                var equipmentName = "";
                if (existingMaintainance != null)
                {
                    _genericEventPublishService.PublishDmsArtifactUsageReferenceDeleteEvent(existingMaintainance);
                    var existingEquipment = await _repository.GetItemAsync<PraxisEquipment>(e =>
                            e.ItemId == existingMaintainance.PraxisEquipmentId && !e.IsMarkedToDelete);
                    equipmentName = existingEquipment?.Name;

                    _praxisEquipmentMaintenanceService.SendMailOnEquipmentMaintenanceDelete(existingMaintainance, equipmentName);
                    await _repository.DeleteAsync<PraxisEquipmentMaintenance>(m => m.ItemId == existingMaintainance.ItemId);
                    _praxisEquipmentMaintenanceService.DeleteDependentEntitiesForEquipmentMaintenance(new List<string> { existingMaintainance.ItemId });

                    var maintainanceList = _repository
                        .GetItems<PraxisEquipmentMaintenance>(m =>
                            m.PraxisEquipmentId == existingMaintainance.PraxisEquipmentId && m.ItemId != existingMaintainance.ItemId && !m.IsMarkedToDelete).ToList();

                    if (existingEquipment != null)
                    {
                        existingEquipment.MaintenanceDates = maintainanceList?.Select(m => new MaintenanceDateProp()
                        {
                            ItemId = m.ItemId,
                            Date = m.MaintenanceEndDate,
                            CompletionStatus = m.CompletionStatus
                        })?.OrderBy(m => m.Date)?.ToList() ?? new List<MaintenanceDateProp>();

                        UpdateMaintenanceDateMetadata(existingEquipment, maintainanceList);
                        UpdateEquipmentData(existingEquipment);
                    }
                    _logger.LogInformation("Data has been successfully deleted from {EntityName} with ItemId: {ItemId} and tenantId: {TenantId}.", nameof(PraxisEquipmentMaintenance), existingMaintainance.ItemId, securityContext.TenantId);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisEquipmentMaintenance)} data with ItemId: {itemId} and tenantId: {securityContext.TenantId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                return false;
            }
        }


        private void UpdateMaintenanceDateMetadata(PraxisEquipment existingEquipment, List<PraxisEquipmentMaintenance> maintainanceList)
        {
            var maintenanceDatesWithType = maintainanceList.Select(m => new MaintenanceDatePropWithType()
            {
                ItemId = m.ItemId,
                Date = m.MaintenanceEndDate,
                CompletionStatus = m.CompletionStatus,
                ScheduleType = m.ScheduleType
            });
            var maintenanceDateMetadata = new MetaDataKeyPairValue
            {
                Key = EquipmentMetaDataKeys.MaintenanceDates,
                MetaData = new MetaValuePair
                {
                    Type = "Array",
                    Value = System.Text.Json.JsonSerializer.Serialize(maintenanceDatesWithType)
                }
            };
            if (existingEquipment.MetaDataList != null && existingEquipment.MetaDataList.Any())
            {
                var eqDatesMetaData = existingEquipment.MetaDataList.FirstOrDefault(m => m.Key == EquipmentMetaDataKeys.MaintenanceDates);
                if (eqDatesMetaData == null) existingEquipment.MetaDataList.Add(maintenanceDateMetadata);
                else
                {
                    eqDatesMetaData.MetaData = maintenanceDateMetadata.MetaData;
                }
            }
            else
            {
                var medataList = new List<MetaDataKeyPairValue>()
                                    {
                                        maintenanceDateMetadata
                                    };
                existingEquipment.MetaDataList = medataList;
            }
        }
        private void UpdateEquipmentData(PraxisEquipment equipment)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            try
            {
                _repository.UpdateAsync(e => e.ItemId == equipment.ItemId, equipment).GetAwaiter();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during update {nameof(PraxisEquipment)} data with ItemId: {equipment.ItemId} and tenantId: {securityContext.TenantId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }
    }
}
