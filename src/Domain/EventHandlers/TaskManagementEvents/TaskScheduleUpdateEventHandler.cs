using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using System;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemEvents;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemCompletionInfoEvents;
using System.Security;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisProcessGuideEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTaskEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.TaskManagementEvents
{
    public class TaskScheduleUpdateEventHandler : IBaseEventHandlerAsync<TaskManagementEvent>
    {
        private readonly IRepository _repository;
        private readonly ILogger<TaskScheduleUpdateEventHandler> _logger;
        private readonly PraxisOpenItemUpdatedEventHandler _praxisOpenItemUpdatedEventHandler;
        private readonly PraxisProcessGuideUpdateEventHandler _praxisProcessGuideUpdateEventHandler;
        private readonly PraxisTaskUpdatedEventHandler _praxisTaskUpdatedEventHandler;

        public TaskScheduleUpdateEventHandler(
            IRepository repository, 
            ILogger<TaskScheduleUpdateEventHandler> logger,
            PraxisOpenItemUpdatedEventHandler praxisOpenItemUpdatedEventHandler,
            PraxisProcessGuideUpdateEventHandler praxisProcessGuideUpdateEventHandler,
            PraxisTaskUpdatedEventHandler praxisTaskUpdatedEventHandler
        )
        {
            _repository = repository;
            _logger = logger;
            _praxisOpenItemUpdatedEventHandler = praxisOpenItemUpdatedEventHandler;
            _praxisProcessGuideUpdateEventHandler = praxisProcessGuideUpdateEventHandler;
            _praxisTaskUpdatedEventHandler = praxisTaskUpdatedEventHandler;
        }

        public async Task<bool> HandleAsync(TaskManagementEvent @event)
        {
            var taskSchudeData = JsonConvert.DeserializeObject<TaskSchedule>(@event.JsonPayload);

            if (taskSchudeData == null || string.IsNullOrEmpty(taskSchudeData?.ItemId)) return false;

            taskSchudeData = await _repository.GetItemAsync<TaskSchedule>(p => p.ItemId == taskSchudeData.ItemId);

            if (taskSchudeData != null &&
                !string.IsNullOrEmpty(taskSchudeData.RelatedEntityId) &&
                !string.IsNullOrEmpty(taskSchudeData.RelatedEntityName))
            {
                if (taskSchudeData.RelatedEntityName.Equals(EntityName.PraxisTask))
                {
                    var task = await _repository.GetItemAsync<PraxisTask>(p => p.ItemId == taskSchudeData.RelatedEntityId);
                    var eventPayload = new GqlEvent<PraxisTask>
                    {
                        EntityData = task,
                        EventName = PraxisEventName.PraxisTaskUpdatedEventName,
                        Filter = $"{{\"_id\": \"{task.ItemId}\"}}"
                };

                    return _praxisTaskUpdatedEventHandler.Handle(eventPayload);
                }
                else if (taskSchudeData.RelatedEntityName.Equals(EntityName.PraxisOpenItem))
                {
                    var task = await _repository.GetItemAsync<PraxisOpenItem>(p => p.ItemId == taskSchudeData.RelatedEntityId);
                    var eventPayload = new GqlEvent<PraxisOpenItem>
                    {
                        EntityData = task,
                        EventName = PraxisEventName.PraxisOpenItemUpdatedEventName,
                        Filter = $"{{\"_id\": \"{task.ItemId}\"}}"
                    };

                    return await _praxisOpenItemUpdatedEventHandler.HandleAsync(eventPayload);
                }
                else if (taskSchudeData.RelatedEntityName.Equals(EntityName.PraxisProcessGuide))
                {
                    var task = await _repository.GetItemAsync<PraxisProcessGuide>(p => p.ItemId == taskSchudeData.RelatedEntityId);
                    var eventPayload = new GqlEvent<PraxisProcessGuide>
                    {
                        EntityData = task,
                        EventName = PraxisEventName.PraxisProcessGuideUpdatedEventName,
                        Filter = $"{{\"_id\": \"{task.ItemId}\"}}"
                    };

                    return await _praxisProcessGuideUpdateEventHandler.HandleAsync(eventPayload);
                }
            }

            return true;
        }
    }
}
