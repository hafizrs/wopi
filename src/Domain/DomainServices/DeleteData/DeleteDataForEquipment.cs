using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataForEquipment : IDeleteDataByCollectionSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteDataForEquipment> _logger;
        private readonly IPraxisEquipmentService _praxisEquipmentService;
        private readonly IPraxisEquipmentMaintenanceService _praxisEquipmentMaintenanceService;
        private readonly IGenericEventPublishService _genericEventPublishService;

        public DeleteDataForEquipment(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            IPraxisEquipmentService praxisEquipmentService,
            ILogger<DeleteDataForEquipment> logger,
            IPraxisEquipmentMaintenanceService praxisEquipmentMaintenanceService,
            IGenericEventPublishService genericEventPublishService
        )
        {
            _securityContextProviderService = securityContextProviderService;
            _repository = repository;
            _logger = logger;
            _praxisEquipmentService = praxisEquipmentService;
            _praxisEquipmentMaintenanceService = praxisEquipmentMaintenanceService;
            _genericEventPublishService = genericEventPublishService;
        }

        public async Task<bool> DeleteData(string entityName, string itemId, string additionalInfosItemId = null, string additionalTitleId = null)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation($"Going to delete {nameof(PraxisEquipment)} related all data with ItemId: {itemId} and tenantId: {securityContext.TenantId}.");
            try
            {
                var existingEquipment = _repository.GetItem<PraxisEquipment>(e => e.ItemId == itemId && !e.IsMarkedToDelete);
                if (existingEquipment != null)
                {
                    _genericEventPublishService.PublishDmsArtifactUsageReferenceDeleteEvent(existingEquipment);
                    await _praxisEquipmentService.DeleteEquipmentFilesAsync(existingEquipment);

                    var existingMaintenances = _repository
                        .GetItems<PraxisEquipmentMaintenance>(em => em.PraxisEquipmentId == existingEquipment.ItemId && !em.IsMarkedToDelete)
                        .ToList();

                    await _repository.DeleteAsync<PraxisEquipment>(pm => pm.ItemId == existingEquipment.ItemId);
                    foreach (var equipmentMaintenance in existingMaintenances)
                    {
                        _praxisEquipmentMaintenanceService.SendMailOnEquipmentMaintenanceDelete(equipmentMaintenance, existingEquipment.Name);
                        await _repository.DeleteAsync<PraxisEquipmentMaintenance>(pm => pm.ItemId == equipmentMaintenance.ItemId);
                        _logger.LogInformation($"Data has been successfully deleted from {nameof(PraxisEquipmentMaintenance)} entity with ItemId: {equipmentMaintenance.ItemId} for {nameof(PraxisEquipment)} ItemId: {existingEquipment.ItemId} and tenantId: {securityContext.TenantId}.");
                    }
                    _praxisEquipmentMaintenanceService.DeleteDependentEntitiesForEquipmentMaintenance(existingMaintenances?.Select(m => m.ItemId)?.ToList());
                    _logger.LogInformation($"Data has been successfully deleted from {nameof(PraxisEquipment)} entity for ItemId: {existingEquipment.ItemId} and TenantId: {securityContext.TenantId}.");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisEquipment)} related all data with ItemId: {itemId} and tenantId: {securityContext.TenantId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                return false;
            }
        }
    }
}
