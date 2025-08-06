using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SWICA;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData;

public class DeleteTaskScheduleDataForPraxisOpenItem : IDeleteTaskScheduleDataStrategy
{
    private readonly ILogger<DeleteTaskScheduleDataForPraxisOpenItem> _logger;
    private readonly ITaskManagementService _taskManagementService;
    private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
    private readonly IRepository _repository;
    private readonly ICockpitFormDocumentActivityMetricsGenerationService _cockpitFormDocumentActivityMetricsGenerationService;

    public DeleteTaskScheduleDataForPraxisOpenItem(
        ILogger<DeleteTaskScheduleDataForPraxisOpenItem> logger,
        ITaskManagementService taskManagementService,
        ICockpitSummaryCommandService cockpitSummaryCommandService,
        IRepository repository,
        ICockpitFormDocumentActivityMetricsGenerationService cockpitFormDocumentActivityMetricsGenerationService)
    {
        _logger = logger;
        _taskManagementService = taskManagementService;
        _cockpitSummaryCommandService = cockpitSummaryCommandService;
        _repository = repository;
        _cockpitFormDocumentActivityMetricsGenerationService = cockpitFormDocumentActivityMetricsGenerationService;
    }

    public async Task<bool> DeleteTask(List<string> itemIds, TaskScheduleRemoveType removeType)
    {
        _logger.LogInformation("Enter {HandlerName} with ItemIds: {ItemIds}.", nameof(DeleteTaskScheduleDataForPraxisOpenItem), JsonConvert.SerializeObject(itemIds));
        try
        {
            var taskSummaryIds = _repository
                .GetItems<PraxisOpenItem>(t => itemIds.Contains(t.ItemId))
                .Select(item => item.TaskSchedule.TaskSummaryId)
                .ToList();
            var updateModel = new
            {
                IsPermanentlyRemove = true,
                RemoveType = removeType,
                TaskSummaryIds = taskSummaryIds
            };
            var isDeleted = await _taskManagementService.RemoveTask(updateModel);
            if (isDeleted)
            {
                _cockpitSummaryCommandService.DeleteCockpitSummaryByTaskSummaryId(taskSummaryIds.ToArray(), nameof(PraxisOpenItem));
                await _cockpitFormDocumentActivityMetricsGenerationService.OnDeleteTaskRemoveSummaryFromActivityMetrics(itemIds, nameof(PraxisOpenItem));
            }
            return isDeleted;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in {HandlerName} with ItemIds: {ItemIds}.", nameof(DeleteTaskScheduleDataForPraxisOpenItem), JsonConvert.SerializeObject(itemIds));
            return false;
        }
    }
}