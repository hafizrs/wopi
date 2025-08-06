using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataForProcessGuide : IDeleteDataByCollectionSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteDataForProcessGuide> _logger;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        public DeleteDataForProcessGuide(ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteDataForProcessGuide> logger,
            ICockpitSummaryCommandService cockpitSummaryCommandService)
        {
            _securityContextProviderService = securityContextProviderService;
            _repository = repository;
            _logger = logger;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
        }
        public async Task<bool> DeleteData(string entityName, string itemId, string additionalInfosItemId = null, string additionalTitleId = null)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation($"Going to delete {nameof(PraxisProcessGuide)} related all data with ItemId: {itemId} and tenantId: {securityContext.TenantId}.");
            try
            {
                var existingProcessGuide = await _repository.GetItemAsync<PraxisProcessGuide>(x => x.ItemId == itemId && !x.IsMarkedToDelete);
                if(existingProcessGuide != null)
                {
                    await _cockpitSummaryCommandService.DeleteSummaryAsync(new List<string> { existingProcessGuide.ItemId },
                        CockpitTypeNameEnum.PraxisProcessGuide);
                    
                    existingProcessGuide.IsMarkedToDelete = true;

                    var processGuideUpdates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", existingProcessGuide.IsMarkedToDelete},
                    };

                    await _repository.UpdateAsync<PraxisProcessGuide>(x => x.ItemId == existingProcessGuide.ItemId, processGuideUpdates);
                    _logger.LogInformation($"Data has been successfully deleted from {nameof(PraxisProcessGuide)} entity for ItemId: {existingProcessGuide.ItemId} and TenantId: {securityContext.TenantId}.");
                }
                return true;
            }
            catch(Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisProcessGuide)} related all data with ItemId: {itemId} and tenantId: {securityContext.TenantId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                return false;
            }
        }
    }
}
