using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteCategoryFromPraxisClientCategory : IDeleteDataByCollectionSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteCategoryFromPraxisClientCategory> _logger;

        public DeleteCategoryFromPraxisClientCategory(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteCategoryFromPraxisClientCategory> logger)
        {
            _securityContextProviderService = securityContextProviderService;
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> DeleteData(string entityName, string itemId, string additionalInfosItemId = null, string additionalTitleId = null)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation($"Going to delete {nameof(PraxisClientCategory)} data for category with ItemId: {itemId} and tenantId: {securityContext.TenantId}.");

            try
            {
                var existingCategory = await _repository.GetItemAsync<PraxisClientCategory>(c => c.ItemId == itemId && !c.IsMarkedToDelete);
                if (existingCategory != null)
                {
                    existingCategory.IsMarkedToDelete = true;

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", existingCategory.IsMarkedToDelete},
                    };

                    await _repository.UpdateAsync<PraxisClientCategory>(c=>c.ItemId==existingCategory.ItemId, updates);
                    _logger.LogInformation($"Data has been successfully updated for {nameof(PraxisClientCategory)} entity with ItemId: {existingCategory.ItemId}.");
                    return true;
                }

                _logger.LogInformation($"No category found in {nameof(PraxisClientCategory)} entity with ItemId: {itemId}.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during update {nameof(PraxisClientCategory)} data with ItemId: {itemId} and tenantId: {securityContext.TenantId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                return false;
            }
        }
    }
}
