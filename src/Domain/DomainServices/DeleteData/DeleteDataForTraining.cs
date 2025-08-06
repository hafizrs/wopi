using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataForTraining : IDeleteDataByCollectionSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteDataForTraining> _logger;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

        public DeleteDataForTraining(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteDataForTraining> logger,
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
            _logger.LogInformation($"Going to delete {nameof(PraxisTraining)} related all data with ItemId: {itemId} and tenantId: {securityContext.TenantId}.");
            try
            {
                var exisitingTraining = await _repository.GetItemAsync<PraxisTraining>(t => t.ItemId == itemId && !t.IsMarkedToDelete);
                if (exisitingTraining != null)
                {
                    DeleteTastManagementData(exisitingTraining.ItemId);
                    await _cockpitSummaryCommandService.DeleteSummaryAsync(new List<string> { exisitingTraining.ItemId }, CockpitTypeNameEnum.PraxisTraining);

                    var existingTrainingAnswers = _repository.GetItems<PraxisTrainingAnswer>(a => a.TrainingId == exisitingTraining.ItemId && !a.IsMarkedToDelete).ToList();

                    foreach (var trainingAnswer in existingTrainingAnswers)
                    {
                        trainingAnswer.IsMarkedToDelete = true;
                        var update = new Dictionary<string, object>
                        {
                            {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                            {"IsMarkedToDelete", trainingAnswer.IsMarkedToDelete},
                        };

                        await _repository.UpdateAsync<PraxisTrainingAnswer>(a => a.ItemId == trainingAnswer.ItemId, update);
                    }

                    exisitingTraining.IsMarkedToDelete = true;
                    var updateTraining = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IsMarkedToDelete", exisitingTraining.IsMarkedToDelete},
                    };

                    await _repository.UpdateAsync<PraxisTraining>(t => t.ItemId == exisitingTraining.ItemId, updateTraining);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisTraining)} related all data with ItemId: {itemId} and tenantId: {securityContext.TenantId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
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
