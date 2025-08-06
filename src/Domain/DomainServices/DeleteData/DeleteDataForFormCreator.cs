using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Events;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataForFormCreator: IDeleteDataByCollectionSpecific
    {

        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteDataForFormCreator> _logger;
        private readonly IPraxisFormService _praxisFormService;
        private readonly IServiceClient _serviceClient;
        private readonly IGenericEventPublishService _genericEventPublishService;

        public DeleteDataForFormCreator(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteDataForFormCreator> logger,
            IPraxisFormService praxisFormService,
            IServiceClient serviceClient,
            IGenericEventPublishService genericEventPublishService
            )
        {
            _securityContextProviderService = securityContextProviderService;
            _repository = repository;
            _logger = logger;
            _praxisFormService = praxisFormService;
            _serviceClient = serviceClient;
            _genericEventPublishService = genericEventPublishService;
        }

        public async Task<bool> DeleteData(string entityName, string itemId, string additionalInfosItemId = null, string additionalTitleId = null)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation("Going to delete {EntityName} data with ItemId: {ItemId} and tenantId: {TenantId}.", nameof(PraxisForm), itemId, securityContext.TenantId);

            try
            {
                var existingForm = await _repository.GetItemAsync<PraxisForm>(f => f.ItemId == itemId && !f.IsMarkedToDelete);
                if (existingForm != null)
                {
                    _genericEventPublishService.PublishDmsArtifactUsageReferenceDeleteEvent(existingForm);
                    _praxisFormService.DeletePraxisFormFilesAsync(existingForm).GetAwaiter().GetResult();

                    existingForm.IsMarkedToDelete = true;
                    existingForm.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
                    existingForm.LastUpdatedBy = securityContext.UserId;
                    
                    await _repository.UpdateAsync(r => r.ItemId == existingForm.ItemId, existingForm);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisForm)} data for FormId:{itemId}. Exception Message: {ex.Message}. Exception Details: {ex.StackTrace}.");
                return false;
            }
        }
    }
}
