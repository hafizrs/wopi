
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.TaskManagementEvents
{
    public class TaskSummaryCreatedEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly ITaskManagementService taskManagementService;
        private readonly ILogger<TaskSummaryCreatedEventHandler> _logger;
        public TaskSummaryCreatedEventHandler(ITaskManagementService taskManagementService, ILogger<TaskSummaryCreatedEventHandler> logger)
        {
            this.taskManagementService = taskManagementService;
            _logger = logger;
        }

        public Task<bool> HandleAsync(GraphQlDataChangeEvent eventData)
        {
            try
            {
                var eventPayload = JsonConvert.DeserializeObject<GqlEvent<TaskSummary>>(eventData.EventTriggeredByJsonPayload);

                if (eventPayload != null)
                {
                    AddRowLevelSecurity(eventPayload);
                }

                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                _logger.LogError("TaskSummaryCreatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return Task.FromResult(false);
        }

        private void AddRowLevelSecurity(GqlEvent<TaskSummary> eventPayload)
        {
            TaskDataBasicDto taskData = JsonConvert.DeserializeObject<TaskDataBasicDto>(eventPayload.EntityData.TaskDataJsonString);

            if (taskData.RelatedEntityName.Equals(EntityName.PraxisTask))
            {
                TaskDataDto<PraxisTask> praxisTaskSummary =
                    JsonConvert.DeserializeObject<TaskDataDto<PraxisTask>>(eventPayload.EntityData.TaskDataJsonString);

                taskManagementService.AddTaskSummaryRowLevelSecurity(
                    praxisTaskSummary.TaskSummaryId, praxisTaskSummary.RelatedEntityObject.ClientId
                );
            }
       
            else if (taskData.RelatedEntityName.Equals(EntityName.PraxisOpenItem))
            {
                TaskDataDto<PraxisOpenItem> praxisOpenItemSummary =
                    JsonConvert.DeserializeObject<TaskDataDto<PraxisOpenItem>>(eventPayload.EntityData.TaskDataJsonString);

                taskManagementService.AddTaskSummaryRowLevelSecurity(
                    praxisOpenItemSummary.TaskSummaryId, praxisOpenItemSummary.RelatedEntityObject.ClientId
                );
            }
        }
    }
}
