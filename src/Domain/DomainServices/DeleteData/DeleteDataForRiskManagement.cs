using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataForRiskManagement : IDeleteDataByCollectionSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteDataForRiskManagement> _logger;

        public DeleteDataForRiskManagement(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteDataForRiskManagement> logger)
        {
            _securityContextProviderService = securityContextProviderService;
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> DeleteData(string entityName, string itemId, string additionalInfosItemId = null, string additionalTitleId = null)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation($"Going to delete {nameof(PraxisRisk)} data with ItemId: {itemId} and tenantId: {securityContext.TenantId}.");

            try
            {
                var existingRisk = await _repository.GetItemAsync<PraxisRisk>(r => r.ItemId == itemId && !r.IsMarkedToDelete);
                if (existingRisk != null)
                {
                    DeleteTastManagementData(existingRisk.ItemId);
                    var existingAssessment =
                        _repository.GetItems<PraxisAssessment>(a => a.RiskId == existingRisk.ItemId && !a.IsMarkedToDelete).ToList();
                    foreach (var assessment in existingAssessment)
                    {
                        assessment.IsMarkedToDelete = true;
                        var updates = new Dictionary<string, object>
                        {
                            {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                            {"IsMarkedToDelete", assessment.IsMarkedToDelete},
                        };

                        await _repository.UpdateAsync<PraxisAssessment>(r => r.ItemId == assessment.ItemId, updates);
                    }

                    existingRisk.IsMarkedToDelete = true;
                    var updateRisk = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", existingRisk.IsMarkedToDelete},
                    };

                    await _repository.UpdateAsync<PraxisRisk>(r => r.ItemId == existingRisk.ItemId, updateRisk);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisRisk)} related all data with ItemId: {itemId} and tenantId: {securityContext.TenantId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                return false;
            }
        }

        private void DeleteTastManagementData(string taskReferenceId)
        {
            var existingPraxisOpenItemConfigList = _repository.GetItems<PraxisOpenItemConfig>(oc => oc.TaskReferenceId == taskReferenceId && !oc.IsMarkedToDelete).ToList();

            foreach (var existingPraxisOpenItemConfig in existingPraxisOpenItemConfigList)
            {
                var distinctSummaryList = _repository
                    .GetItems<PraxisOpenItem>(ot => ot.OpenItemConfigId == existingPraxisOpenItemConfig.ItemId && !ot.IsMarkedToDelete)
                    .GroupBy(g => g.TaskSchedule.TaskSummaryId)
                    .ToList();
                var existingPraxisOpenItems = _repository
                    .GetItems<PraxisOpenItem>(ot => ot.OpenItemConfigId == existingPraxisOpenItemConfig.ItemId && !ot.IsMarkedToDelete)
                    .ToList();

                UpdateOpenItemScheduleData(existingPraxisOpenItems);

                foreach (var summary in distinctSummaryList)
                {
                    var existingSummary = _repository.GetItem<TaskSummary>(s => s.ItemId == summary.Key && !s.IsMarkedToDelete);
                    if (existingSummary != null)
                    {
                        existingSummary.IsMarkedToDelete = true;
                        var update = new Dictionary<string, object>
                        {
                            {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                            {"IsMarkedToDelete", existingSummary.IsMarkedToDelete},
                        };

                        _repository.UpdateAsync<TaskSummary>(s => s.ItemId == existingSummary.ItemId, update).GetAwaiter();
                    }
                }

                existingPraxisOpenItemConfig.IsMarkedToDelete = true;
                var updateOpenItemConfig = new Dictionary<string, object>
                {
                    {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                    {"IsMarkedToDelete", existingPraxisOpenItemConfig.IsMarkedToDelete},
                };

                _repository.UpdateAsync<PraxisOpenItemConfig>(oc => oc.ItemId == existingPraxisOpenItemConfig.ItemId, updateOpenItemConfig).GetAwaiter();
            }
        }

        private void UpdateOpenItemScheduleData(List<PraxisOpenItem> existingPraxisOpenItems)
        {
            foreach (var praxisOpenItem in existingPraxisOpenItems)
            {
                praxisOpenItem.IsMarkedToDelete = true;
                var update = new Dictionary<string, object>
                {
                    {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                    {"IsMarkedToDelete", praxisOpenItem.IsMarkedToDelete},
                };

                _repository.UpdateAsync<PraxisOpenItem>(o => o.ItemId == praxisOpenItem.ItemId, update).GetAwaiter();

                var existingSchedule = _repository.GetItem<TaskSchedule>(s => s.ItemId == praxisOpenItem.TaskSchedule.ItemId && !s.IsMarkedToDelete);
                if (existingSchedule != null)
                {
                    existingSchedule.IsMarkedToDelete = true;
                    var updateSchedule = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", existingSchedule.IsMarkedToDelete},
                    };

                    _repository.UpdateAsync<PraxisOpenItem>(o => o.ItemId == existingSchedule.ItemId, updateSchedule).GetAwaiter();
                }
            }
        }
    }
}
