using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Domain.Notifier;
using Newtonsoft.Json;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTaskEvents
{
    public class PraxisTaskCreatedEventHandler : IBaseEventHandler<GqlEvent<PraxisTask>>
    {
        private readonly IPraxisTaskService praxisTaskService;
        private readonly ITaskManagementService taskManagementService;
        private readonly ILogger<PraxisTaskCreatedEventHandler> _logger;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly INotificationService _notificationService;
        public PraxisTaskCreatedEventHandler(IPraxisTaskService praxisTaskService, 
            ITaskManagementService taskManagementService,
            ILogger<PraxisTaskCreatedEventHandler> logger,
            ICockpitSummaryCommandService cockpitSummaryCommandService,
            INotificationService notificationService
        )
        {
            this.praxisTaskService = praxisTaskService;
            this.taskManagementService = taskManagementService;
            _logger = logger;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _notificationService = notificationService;
        }
        public bool Handle(GqlEvent<PraxisTask> eventPayload)
        {
            try
            {
                string clientId = eventPayload.EntityData.ClientId;
                string taskItemId = eventPayload.EntityData.ItemId;


                AddPraxisTaskRowLevelSecurity(taskItemId, clientId);

                TaskSchedule taskSchedule = eventPayload.EntityData.TaskSchedule;

                if (taskSchedule != null)
                {
                    AddTaskScheduleRowLevelSecurity(taskSchedule.ItemId, clientId);
                }

                if (!string.IsNullOrEmpty(eventPayload?.EntityData?.ItemId))
                {
                    var denormalizePayload = JsonConvert.SerializeObject(new
                    {
                        TaskId = eventPayload?.EntityData?.ItemId
                    });
                    _notificationService.GetCommonSubscriptionNotification(
                        true,
                        eventPayload.EntityData.TaskConfigId,
                        "PraxisTaskCreated",
                        "PraxisTaskCreated",
                        denormalizePayload
                    ).GetAwaiter().GetResult();
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("PraxisTaskCreatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return false;
        }

        private void AddPraxisTaskRowLevelSecurity(string taskId, string clientId)
        {
            praxisTaskService.AddRowLevelSecurity(taskId, clientId);
        }
        private void AddTaskScheduleRowLevelSecurity(string taskScheduleId, string clientId)
        {
            taskManagementService.AddTaskScheduleRowLevelSecurity(taskScheduleId, clientId);
        }
    }
}
