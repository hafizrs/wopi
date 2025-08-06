using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataForPraxisAssessment : IDeleteDataByCollectionSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteDataForPraxisAssessment> _logger;

        public DeleteDataForPraxisAssessment(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteDataForPraxisAssessment> logger)
        {
            _securityContextProviderService = securityContextProviderService;
            _repository = repository;
            _logger = logger;
        }
        public async Task<bool> DeleteData(string entityName, string itemId, string additionalInfosItemId = null, string additionalTitleId = null)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation("Going to delete {EntityName} related all data with ItemId: {ItemId} and tenantId: {TenantId}.", nameof(PraxisAssessment), itemId, securityContext.TenantId);
            try
            {
                var existingAssessment = await _repository.GetItemAsync<PraxisAssessment>(a => a.ItemId == itemId && !a.IsMarkedToDelete);
                if (existingAssessment != null)
                {
                    var assessmentList = _repository
                        .GetItems<PraxisAssessment>(a => a.RiskId == existingAssessment.RiskId && !a.IsMarkedToDelete)
                        .ToList();
                    if (assessmentList.Count > 1)
                    {
                        var existingRisk = _repository.GetItem<PraxisRisk>(r => r.ItemId == existingAssessment.RiskId && !r.IsMarkedToDelete);
                        if (existingRisk.RecentAssessment.ItemId == itemId)
                        {
                            var recentAssessment = assessmentList.Where(a => a.ItemId != existingAssessment.ItemId)
                                .OrderByDescending(o => o.CreateDate).FirstOrDefault();
                            if (recentAssessment != null)
                            {
                                existingRisk.LastUpdateDate=DateTime.UtcNow.ToLocalTime();
                                existingRisk.RecentAssessment = recentAssessment;
                                RemoveAssessmentAndUpDateRiskData(existingRisk, existingAssessment.ItemId);
                            }
                        }
                        else
                        {
                            existingAssessment.IsMarkedToDelete = true;
                            var updateAssessment = new Dictionary<string, object>
                            {
                                {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                                {"IsMarkedToDelete", existingAssessment.IsMarkedToDelete},
                            };

                            await _repository.UpdateAsync<PraxisAssessment>(a => a.ItemId == existingAssessment.ItemId, updateAssessment);
                        }
                    }
                }
                _logger.LogInformation("Data has been successfully deleted from {EntityName} entity with ItemId: {ItemId} for tenantId: {TenantId}.", nameof(PraxisAssessment), itemId, securityContext.TenantId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisAssessment)} related all data with ItemId: {itemId} and tenantId: {securityContext.TenantId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                return false;
            }
        }

        private void RemoveAssessmentAndUpDateRiskData(PraxisRisk existingRisk, string assessmentItemId)
        {
            _repository.UpdateAsync<PraxisRisk>(r => r.ItemId == existingRisk.ItemId, existingRisk).GetAwaiter();
            _logger.LogInformation("Data has been successfully deleted from {EntityName} entity with ItemId: {ItemId}.", nameof(PraxisRisk), existingRisk.ItemId);

            var updateAssessment = new Dictionary<string, object>
            {
                {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                {"IsMarkedToDelete", true},
            };

            _repository.UpdateAsync<PraxisAssessment>(a => a.ItemId == assessmentItemId, updateAssessment).GetAwaiter();
            _logger.LogInformation("Data has been successfully deleted from {EntityName} entity with ItemId: {ItemId}.", nameof(PraxisAssessment), assessmentItemId);
        }
    }
}
