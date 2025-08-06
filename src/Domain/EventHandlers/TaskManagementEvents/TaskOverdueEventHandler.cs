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

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.TaskManagementEvents
{
    public class TaskOverdueEventHandler : IBaseEventHandlerAsync<TaskManagementEvent>
    {
        private readonly IEmailNotifierService emailNotifierService;
        private readonly IRepository repository;
        private readonly IEmailDataBuilder emailDataBuilder;
        private readonly IPraxisTaskService praxisTaskService;
        private readonly ILogger<TaskOverdueEventHandler> _logger;

        public TaskOverdueEventHandler(
            IEmailNotifierService emailNotifierService, 
            IRepository repository, 
            IEmailDataBuilder emailDataBuilder,
            IPraxisTaskService praxisTaskService,
            ILogger<TaskOverdueEventHandler> logger

        )
        {
            this.emailNotifierService = emailNotifierService;
            this.repository = repository;
            this.emailDataBuilder = emailDataBuilder;
            this.praxisTaskService = praxisTaskService;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(TaskManagementEvent @event)
        {
            TaskManagementOverdueEvent overdueEventData = JsonConvert.DeserializeObject<TaskManagementOverdueEvent>(@event.JsonPayload);

            if (overdueEventData == null || overdueEventData.TaskSchedule == null || string.IsNullOrEmpty(overdueEventData.PersonId)) return false;
           
            var taskSummary = repository.GetItem<TaskSummary>(p => p.ItemId.Equals(overdueEventData.TaskSchedule.TaskSummaryId) && !p.IsMarkedToDelete);

            if ( taskSummary != null &&
                !string.IsNullOrEmpty(overdueEventData.TaskSchedule.RelatedEntityId) &&
                !string.IsNullOrEmpty(overdueEventData.TaskSchedule.RelatedEntityName))
            {
                if (overdueEventData.TaskSchedule.RelatedEntityName.Equals(EntityName.PraxisTask))
                {
                    return await ProcessPraxisTaskOverdue(overdueEventData.TaskSchedule, taskSummary, overdueEventData.PersonId);
                }
                else if (overdueEventData.TaskSchedule.RelatedEntityName.Equals(EntityName.PraxisOpenItem))
                {
                    return await ProcessPraxisOpenItemOverdue(overdueEventData.TaskSchedule, taskSummary, overdueEventData.PersonId);
                }
                else if (overdueEventData.TaskSchedule.RelatedEntityName.Equals(EntityName.PraxisProcessGuide))
                {
                    return await ProcessPraxisProcessGuideOverDue(overdueEventData.TaskSchedule, overdueEventData.PersonId);
                }
            }

            return true;
        }

        private async Task<bool> ProcessPraxisOpenItemOverdue(TaskSchedule taskSchedule, TaskSummary taskSummary, string personId)
        {
            PraxisOpenItem openItem =repository.GetItem<PraxisOpenItem>(po => po.ItemId.Equals(taskSchedule.RelatedEntityId) && !po.IsMarkedToDelete);

            if (openItem == null || taskSummary == null) return false;

            var clientName = string.Empty;
            var client = repository.GetItem<PraxisClient>(c => c.ItemId == openItem.ClientId);
            if (client != null)
            {
                clientName = client.ClientName;
            }

            var taskStatusList = new List<Task<bool>>();

            if (!taskSchedule.IsCompleted && taskSchedule.TaskMovedDates.Any() && openItem.ControlledMembers.Contains(personId))
            {
                var overdueEmailStatus = SendOpenItemOverdueEmail(personId, taskSummary, openItem, clientName);
                taskStatusList.Add(overdueEmailStatus);
                 
            }

            await Task.WhenAll(taskStatusList);

            return true;
        }
        private async Task<bool> ProcessPraxisTaskOverdue(TaskSchedule taskSchedule, TaskSummary taskSummary, string personId)
        {
            PraxisTask praxisTask = praxisTaskService.GetPraxisTask(taskSchedule.RelatedEntityId);
            PraxisTaskConfig taskConfig =
                repository.GetItem<PraxisTaskConfig>(tc => tc.ItemId.Equals(praxisTask.TaskConfigId) && !tc.IsMarkedToDelete);

            if (praxisTask == null || taskConfig == null) return false;
            
            var taskStatusList = new List<Task<bool>>();

            var clientName = string.Empty;
            var client = repository.GetItem<PraxisClient>(c => c.ItemId == praxisTask.ClientId);
            if (client != null)
            {
                clientName = client.ClientName;
            }

            if (praxisTask.TaskFulfillmentPercentage > praxisTask.TaskSchedule.TaskPercentage && taskSchedule.IsCompleted &&
                taskConfig.TaskNotification.TaskNotFullFilled.Members.Contains(personId))
            {
                var fulfillmentMailTask = SendTaskNotFulfilledEmail(personId, taskSummary, praxisTask, clientName);
                taskStatusList.Add(fulfillmentMailTask);
            }

            if (!taskSchedule.IsCompleted && taskSchedule.TaskMovedDates != null && taskSchedule.TaskMovedDates.Any() && 
                taskConfig.TaskNotification.TaskToNextDay.Members.Contains(personId))
            {
                var rescheduleMailTask = SendTaskRescheduledEmail(personId, taskSchedule, taskSummary, praxisTask, clientName);
                taskStatusList.Add(rescheduleMailTask);
            }

            await Task.WhenAll(taskStatusList);

            return true;
        }

        private async Task<bool> SendTaskNotFulfilledEmail(string personId, TaskSummary taskSummary, PraxisTask task, string clientName)
        {
            var person = repository.GetItem<Person>(p => p.ItemId.Equals(personId) && !p.IsMarkedToDelete);

            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                var emailData = emailDataBuilder.BuildTaskSummaryEmailData(taskSummary, person, task, clientName);
                return await emailNotifierService.SendTaskNotFulfilledEmail(person, emailData);
            }

            return false;
        }
        private async Task<bool> SendTaskRescheduledEmail(string personId, TaskSchedule taskSchedule, TaskSummary taskSummary, PraxisTask task, string clientName)
        {
            var person = repository.GetItem<Person>(p => p.ItemId.Equals(personId) && !p.IsMarkedToDelete);

            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                var emailData = emailDataBuilder.BuildTaskRescheduledEmailData(taskSummary, taskSchedule, person, task, clientName);
                return await emailNotifierService.SendTaskRescheduledEmail(person, emailData);
            }

            return false;
        }
        private async Task<bool> SendOpenItemOverdueEmail(string personId, TaskSummary taskSummary, PraxisOpenItem openItem, string clientName)
        {
            _logger.LogError("Over Due Open Item Mail Sending start {PersonId}", personId);
            var person = repository.GetItem<Person>(p => p.ItemId.Equals(personId) && !p.IsMarkedToDelete);

            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                var emailData = emailDataBuilder.BuildTaskSummaryEmailData(taskSummary, person, openItem, clientName);
                return await emailNotifierService.SendTaskOverdueEmail(person, emailData);
            }
     
            return false;
        }

        private async Task<bool> ProcessPraxisProcessGuideOverDue(TaskSchedule taskSchedule, string personId)
        {
            PraxisProcessGuide praxisProcessGuide = repository.GetItem<PraxisProcessGuide>(po => po.ItemId.Equals(taskSchedule.RelatedEntityId) && !po.IsMarkedToDelete);

            if (praxisProcessGuide == null) return false;

            var taskStatusList = new List<Task<bool>>();

            var clientName = string.Empty;
            var client = repository.GetItem<PraxisClient>(c => c.ItemId == praxisProcessGuide.ClientId);
            if (client != null)
            {
                clientName = client.ClientName;
            }

            DateTime dueDate = taskSchedule.TaskDateTime.Date.AddDays(1).AddSeconds(-1);

            if (DateTime.Now > dueDate)
            {
                var rescheduleMailTask = SendProcessGuideOverDueEmail(personId, dueDate, praxisProcessGuide, clientName);
                taskStatusList.Add(rescheduleMailTask);
            }

            await Task.WhenAll(taskStatusList);

            return true;
        }

        private async Task<bool> SendProcessGuideOverDueEmail(string personId, DateTime dueDate, PraxisProcessGuide praxisProcessGuide, string clientName)
        {
            var person = repository.GetItem<Person>(p => p.ItemId.Equals(personId) && !p.IsMarkedToDelete);

            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                var emailData = emailDataBuilder.BuildProcessGuideOverDueEmailData(person, dueDate, praxisProcessGuide, clientName);
                return await emailNotifierService.SendProcessGuideOverDueEmail(person, emailData);
            }

            return false;
        }
    }
}
