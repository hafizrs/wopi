using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteRiskDataForSystemAdmin : IDeleteDataByRoleSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteRiskDataForSystemAdmin> _logger;

        public DeleteRiskDataForSystemAdmin(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteRiskDataForSystemAdmin> logger)
        {
            _securityContextProviderService = securityContextProviderService;
            _repository = repository;
            _logger = logger;
        }

        public bool DeleteData(string itemId)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation($"Going to delete {nameof(PraxisRisk)} data with ItemId: {itemId} for admin.");
            try
            {
                var existingRisk = _repository.GetItem<PraxisRisk>(r => r.ItemId == itemId);
                if (existingRisk != null)
                {
                    DeleteTastManagementData(existingRisk.ItemId);
                    var existingAssessment =
                        _repository.GetItems<PraxisAssessment>(a => a.RiskId == existingRisk.ItemId).ToList();
                    foreach (var assessment in existingAssessment)
                    {
                        _repository.Delete<PraxisAssessment>(a => a.ItemId == assessment.ItemId);
                    }
                    _repository.Delete<PraxisRisk>(r => r.ItemId == existingRisk.ItemId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisRisk)} related all data with ItemId: {itemId} and tenantId: {securityContext.TenantId} for Admin role. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                return false;
            }
        }

        private void DeleteTastManagementData(string taskReferenceId)
        {
            var existingPraxisOpenItemConfig =
                _repository.GetItem<PraxisOpenItemConfig>(oc => oc.TaskReferenceId == taskReferenceId);
            if (existingPraxisOpenItemConfig != null)
            {
                var distinctSummaryList = _repository
                    .GetItems<PraxisOpenItem>(ot => ot.OpenItemConfigId == existingPraxisOpenItemConfig.ItemId)
                    .GroupBy(g => g.TaskSchedule.TaskSummaryId)
                    .ToList();
                var existingPraxisOpenItems = _repository
                    .GetItems<PraxisOpenItem>(ot => ot.OpenItemConfigId == existingPraxisOpenItemConfig.ItemId)
                    .ToList();
                foreach (var praxisOpenItem in existingPraxisOpenItems)
                {
                    _repository.Delete<TaskSchedule>(s => s.ItemId == praxisOpenItem.TaskSchedule.ItemId);
                    _repository.Delete<PraxisOpenItem>(o => o.ItemId == praxisOpenItem.ItemId);
                }

                foreach (var summary in distinctSummaryList)
                {
                    _repository.Delete<TaskSummary>(s => s.ItemId == summary.Key);
                }
                _repository.Delete<PraxisOpenItemConfig>(oc => oc.ItemId == existingPraxisOpenItemConfig.ItemId);
            }
        }
    }
}
