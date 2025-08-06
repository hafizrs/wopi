using System;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteCategoryDataForSystemAdmin : IDeleteDataByRoleSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteCategoryDataForSystemAdmin> _logger;

        public DeleteCategoryDataForSystemAdmin(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteCategoryDataForSystemAdmin> logger)
        {
            _securityContextProviderService = securityContextProviderService;
            _repository = repository;
            _logger = logger;
        }
        public bool DeleteData(string itemId)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation("Going to delete {EntityName} entity Category data with ItemId: {ItemId} for admin.", nameof(PraxisClientCategory), itemId);
            try
            {
                _repository.Delete<PraxisClientCategory>(c => c.ItemId == itemId);
                _logger.LogInformation("Delete has been successfully done for {EntityName} entity Category data with ItemId: {ItemId} for admin.", nameof(PraxisClientCategory), itemId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisClientCategory)} entity Category data with ItemId: {itemId} and tenantId: {securityContext.TenantId} for Admin role. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                return false;
            }
        }
    }
}
