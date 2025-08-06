using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using SeliseBlocks.GraphQL.Models;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.TaskManagementEvents
{
    public class TaskScheduleCreatedEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly ITaskManagementService taskManagementService;
        private readonly IPraxisTaskService praxisTaskService;
        private readonly ILogger<TaskScheduleCreatedEventHandler> _logger;
        private readonly IRepository repository;
        public TaskScheduleCreatedEventHandler(ITaskManagementService taskManagementService,
            IPraxisTaskService praxisTaskService,
            ILogger<TaskScheduleCreatedEventHandler> logger,
            IRepository repository
        )
        {
            this.taskManagementService = taskManagementService;
            this.praxisTaskService = praxisTaskService;
            this.repository = repository;
            _logger = logger;
        }

        public Task<bool> HandleAsync(GraphQlDataChangeEvent eventData)

        {
            try
            {
                var eventPayload = JsonConvert.DeserializeObject<GqlEvent<TaskSchedule>>(eventData.EventTriggeredByJsonPayload);

                if (eventPayload != null)
                {
                    AddRowLevelSecurity(eventPayload);
                }

                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                _logger.LogError("TaskScheduleCreatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return Task.FromResult(false);
        }

        private void AddRowLevelSecurity(GqlEvent<TaskSchedule> eventPayload)
        {
            if (eventPayload.EntityData.RelatedEntityName!=null && eventPayload.EntityData.RelatedEntityName.Equals(EntityName.PraxisTask))
            {
                PraxisTask task = praxisTaskService.GetPraxisTask(eventPayload.EntityData.RelatedEntityId);

                if (task != null)
                {
                    taskManagementService.AddTaskScheduleRowLevelSecurity(
                        eventPayload.EntityData.ItemId, task.ClientId
                    );
                }
            }
            else if (eventPayload.EntityData.RelatedEntityName != null && eventPayload.EntityData.RelatedEntityName.Equals(EntityName.PraxisOpenItem))
            {
                PraxisOpenItem openItem =
                    repository.GetItem<PraxisOpenItem>(po => po.ItemId.Equals(eventPayload.EntityData.RelatedEntityId) && !po.IsMarkedToDelete);

                if (openItem != null)
                {
                    taskManagementService.AddTaskScheduleRowLevelSecurity(
                        eventPayload.EntityData.ItemId, openItem.ClientId
                    );
                }
            }
        }
    }
}
