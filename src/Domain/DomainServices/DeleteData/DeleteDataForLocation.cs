using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataForLocation : IDeleteDataByCollectionSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteDataForLocation> _logger;
        private readonly IPraxisEquipmentService _praxisEquipmentService;

        public DeleteDataForLocation(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteDataForLocation> logger,
            IPraxisEquipmentService praxisEquipmentService
            )
        {
            _securityContextProviderService = securityContextProviderService;
            _repository = repository;
            _logger = logger;
            _praxisEquipmentService = praxisEquipmentService;
        }
        public async Task<bool> DeleteData(string entityName, string itemId, string additionalInfosItemId = null, string additionalTitleId = null)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation("Going to delete {EntityName} related all data with ItemId: {ItemId} and TenantId: {TenantId}.", nameof(PraxisRoom), itemId, securityContext.TenantId);
            try
            {
                var existingRoom = await _repository.GetItemAsync<PraxisRoom>(r => r.ItemId == itemId && !r.IsMarkedToDelete);
                if (existingRoom != null)
                {
                    var deletedEquipmentList = DeleteAllEquipment(existingRoom.ItemId);
                    DeleteAllMaintainanceData(deletedEquipmentList,existingRoom.ItemId);

                    existingRoom.IsMarkedToDelete = true;
                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", existingRoom.IsMarkedToDelete},
                    };

                    await _repository.UpdateAsync<PraxisRoom>(r => r.ItemId == existingRoom.ItemId, updates);
                    _logger.LogInformation("Data has been successfully deleted from {EntityName} with ItemId: {ItemId} and TenantId: {TenantId}.", nameof(PraxisRoom), existingRoom.ItemId, securityContext.TenantId);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisRoom)} related all data with ItemId: {itemId} and tenantId: {securityContext.TenantId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                return false;
            }
        }

        private List<string> DeleteAllEquipment(string roomId)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            var equipmentIds = new List<string>();
            var existingEquipments = _repository.GetItems<PraxisEquipment>(eq => eq.RoomId == roomId && !eq.IsMarkedToDelete).ToList();
            foreach (var equipment in existingEquipments)
            {
                try
                {
                    _praxisEquipmentService.DeleteEquipmentFilesAsync(equipment).GetAwaiter();
                    equipment.IsMarkedToDelete = true;
                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", equipment.IsMarkedToDelete},
                    };

                    _repository.UpdateAsync<PraxisEquipment>(eq => eq.ItemId == equipment.ItemId, updates).GetAwaiter();
                    _logger.LogInformation("Data has been successfully deleted from {EntityName} with ItemId: {ItemId} for RoomId: {RoomId} and TenantId: {TenantId}.", nameof(PraxisEquipment), equipment.ItemId, roomId, securityContext.TenantId);
                    equipmentIds.Add(equipment.ItemId);

                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"Exception occured during delete {nameof(PraxisEquipment)} entity data with ItemId: {equipment.ItemId} and tenantId: {securityContext.TenantId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                }
            }

            return equipmentIds;
        }

        private void DeleteAllMaintainanceData(List<string> equipmentIds, string roomId)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();

            var existingMaintainanceList = _repository
                .GetItems<PraxisEquipmentMaintenance>(m =>equipmentIds.Contains(m.PraxisEquipmentId) && !m.IsMarkedToDelete).ToList();
            foreach (var equipmentMaintenance in existingMaintainanceList)
            {
                try
                {
                    equipmentMaintenance.IsMarkedToDelete = true;
                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", equipmentMaintenance.IsMarkedToDelete},
                    };

                    _repository.UpdateAsync<PraxisEquipmentMaintenance>(m => m.ItemId == equipmentMaintenance.ItemId, updates).GetAwaiter();
                    _logger.LogInformation("Data has been successfully deleted from {EntityName} with ItemId: {ItemId} for RoomId: {RoomId} and TenantId: {TenantId}.", nameof(PraxisEquipmentMaintenance), equipmentMaintenance.ItemId, roomId, securityContext.TenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"Exception occured during delete {nameof(PraxisEquipmentMaintenance)} entity data with ItemId: {equipmentMaintenance.ItemId} and tenantId: {securityContext.TenantId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                }
            }
        }
    }
}
