using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.TaskManagementEvents
{
    public class TaskAssignedEventHandler : IBaseEventHandlerAsync<TaskManagementEvent>
    {
        private readonly IEmailNotifierService emailNotifierService;
        private readonly IRepository repository;
        private readonly IEmailDataBuilder emailDataBuilder;

        public TaskAssignedEventHandler(
            IEmailNotifierService emailNotifierService,
            IRepository repository,
            IEmailDataBuilder emailDataBuilder
        )
        {
            this.emailNotifierService = emailNotifierService;
            this.repository = repository;
            this.emailDataBuilder = emailDataBuilder;
        }

        public async Task<bool> HandleAsync(TaskManagementEvent @event)
        {
            TaskManagementAssignEvent assignEventData = JsonConvert.DeserializeObject<TaskManagementAssignEvent>(@event.JsonPayload);

            if (assignEventData == null || assignEventData.TaskSummary == null || string.IsNullOrEmpty(assignEventData.PersonId)) return false;

            TaskDataBasicDto taskData = JsonConvert.DeserializeObject<TaskDataBasicDto>(assignEventData.TaskSummary.TaskDataJsonString);

            if (taskData.RelatedEntityName.Equals(EntityName.PraxisOpenItem))
            {
                TaskDataDto<PraxisOpenItem> praxisOpenItemSummary =
                    JsonConvert.DeserializeObject<TaskDataDto<PraxisOpenItem>>(assignEventData.TaskSummary.TaskDataJsonString);

                if (praxisOpenItemSummary == null) return false;

                return await SendTaskAssignedEmail(assignEventData.PersonId,
                    praxisOpenItemSummary.RelatedEntityObject, assignEventData.TaskSummary);
            }

            return true;
        }

        private async Task<bool> SendTaskAssignedEmail(string personId, PraxisOpenItem openItem, TaskSummary taskSummary)
        {
            var person = repository.GetItem<Person>(p => p.ItemId.Equals(personId) && !p.IsMarkedToDelete);

            PraxisOpenItem openItemData = repository.GetItem<PraxisOpenItem>(
                po => po.OpenItemConfigId.Equals(openItem.OpenItemConfigId) && po.TaskId.Equals(openItem.TaskId) && !po.IsMarkedToDelete);

            string clientName = repository.GetItem<PraxisClient>(x => x.ItemId == openItemData.ClientId).ClientName;

            if (!string.IsNullOrWhiteSpace(person?.Email) && openItemData != null)
            {
                var user = repository.GetItem<PraxisUser>(c => c.ItemId == openItem.CreatedBy);
                string assignedBy = user?.DisplayName ?? string.Empty;

                var emailData = emailDataBuilder.BuildTaskSummaryEmailData(taskSummary, person, openItemData, clientName, false, assignedBy);

                if (!string.IsNullOrEmpty(openItemData.TaskReference.Key) && openItemData.TaskReference.Key.Equals("risk-management"))
                {
                    return await emailNotifierService.SendTaskAssignedEmail(
                        person, emailData, EmailTemplateName.TaskAssigned.ToString()
                    );
                }
                else if (!string.IsNullOrEmpty(openItemData.TaskReference.Key) && openItemData.TaskReference.Key.Equals("training"))
                {
                    emailData = emailDataBuilder.BuildTaskSummaryEmailData(taskSummary, person, openItemData, clientName, true, assignedBy);

                    return await emailNotifierService.SendTaskAssignedEmail(
                        person, emailData, EmailTemplateName.TaskAssigned.ToString()
                    );
                }
                else
                {
                    return await emailNotifierService.SendTaskAssignedEmail(
                        person, emailData, EmailTemplateName.TaskAssigned.ToString()
                    );
                }
            }
            return true;
        }
    }
}