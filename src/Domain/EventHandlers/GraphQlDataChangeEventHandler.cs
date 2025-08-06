using System;
using System.Threading.Tasks;
using SeliseBlocks.GraphQL.Models;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.TaskManagementEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class GraphQlDataChangeEventHandler : IEventHandler<GraphQlDataChangeEvent, bool>
    {
        private readonly PraxisOrganizationEventHandler _praxisOrganizationEventHandler;
        private readonly PraxisClientEventHandler _praxisClientEventHandler;
        private readonly PraxisClientCategoryEventHandler _praxisClientCategoryEventHandler;
        private readonly PraxisTaskEventHandler _praxisTaskEventHandler;
        private readonly PraxisTrainingEventHandler _praxisTrainingEventHandler;
        private readonly PraxisTrainingAnswerEventHandler _praxisTrainingAnswerEventHandler;
        private readonly PraxisTaskConfigEventHandler _praxisTaskConfigEventHandler;
        private readonly PraxisFormEventHandler _praxisFormEventHandler;
        private readonly TaskSummaryCreatedEventHandler _taskSummaryCreatedEventHandler;
        private readonly TaskScheduleCreatedEventHandler _taskScheduleCreatedEventHandler;
        private readonly PraxisUserEventHandler _praxisUserEventHandler;
        private readonly PraxisRiskEventHandler _praxisRiskEventHandler;
        private readonly PraxisAssessmentEventHandler _praxisAssessmentEventHandler;
        private readonly PraxisEquipmentMaintenanceEventHandler _praxisEquipmentMaintenanceEventHandler;
        private readonly PraxisEquipmentEventHandler _praxisEquipmentEventHandler;
        private readonly PraxisRoomEventHandler _praxisRoomEventHandler;
        private readonly PraxisOpenItemEventHandler _praxisOpenItemEventHandler;
        private readonly PraxisOpenItemConfigEventHandler _praxisOpenItemConfigEventHandler;
        private readonly PraxisOpenItemCompletionInfoEventHandler _praxisCompletionInfoEventHandler;
        private readonly PraxisProcessGuideEventHandler _praxisProcessGuideEventHandler;
        private readonly PraxisProcessGuideAnswerEventHandler _praxisProcessGuideAnswerEventHandler;

        public GraphQlDataChangeEventHandler(
            PraxisOrganizationEventHandler praxisOrganizationEventHandler,
            PraxisClientEventHandler praxisClientEventHandler,
            PraxisClientCategoryEventHandler praxisClientCategoryEventHandler,
            PraxisTaskEventHandler praxisTaskEventHandler,
            PraxisTrainingEventHandler praxisTrainingEventHandler,
            PraxisTrainingAnswerEventHandler praxisTrainingAnswerEventHandler,
            PraxisTaskConfigEventHandler praxisTaskConfigEventHandler,
            PraxisFormEventHandler praxisFormEventHandler,
            TaskSummaryCreatedEventHandler taskSummaryCreatedEventHandler,
            TaskScheduleCreatedEventHandler taskScheduleCreatedEventHandler,
            PraxisUserEventHandler praxisUserEventHandler,
            PraxisRiskEventHandler praxisRiskEventHandler,
            PraxisAssessmentEventHandler praxisAssessmentEventHandler,
            PraxisEquipmentMaintenanceEventHandler praxisEquipmentMaintenanceEventHandler,
            PraxisEquipmentEventHandler praxisEquipmentEventHandler,
            PraxisRoomEventHandler praxisRoomEventHandler,
            PraxisOpenItemEventHandler praxisOpenItemEventHandler,
            PraxisOpenItemConfigEventHandler praxisOpenItemConfigEventHandler,
            PraxisOpenItemCompletionInfoEventHandler praxisCompletionInfoEventHandler,
            PraxisProcessGuideEventHandler praxisProcessGuideEventHandler,
            PraxisProcessGuideAnswerEventHandler praxisProcessGuideAnswerEventHandler)
        {
            _praxisOrganizationEventHandler = praxisOrganizationEventHandler;
            _praxisClientEventHandler = praxisClientEventHandler;
            _praxisClientCategoryEventHandler = praxisClientCategoryEventHandler;
            _praxisTaskEventHandler = praxisTaskEventHandler;
            _praxisTrainingEventHandler = praxisTrainingEventHandler;
            _praxisTrainingAnswerEventHandler = praxisTrainingAnswerEventHandler;
            _praxisTaskConfigEventHandler = praxisTaskConfigEventHandler;
            _praxisFormEventHandler = praxisFormEventHandler;
            _taskSummaryCreatedEventHandler = taskSummaryCreatedEventHandler;
            _taskScheduleCreatedEventHandler = taskScheduleCreatedEventHandler;
            _praxisUserEventHandler = praxisUserEventHandler;
            _praxisRiskEventHandler = praxisRiskEventHandler;
            _praxisAssessmentEventHandler = praxisAssessmentEventHandler;
            _praxisEquipmentMaintenanceEventHandler = praxisEquipmentMaintenanceEventHandler;
            _praxisEquipmentEventHandler = praxisEquipmentEventHandler;
            _praxisRoomEventHandler = praxisRoomEventHandler;
            _praxisOpenItemConfigEventHandler = praxisOpenItemConfigEventHandler;
            _praxisCompletionInfoEventHandler = praxisCompletionInfoEventHandler;
            _praxisOpenItemEventHandler = praxisOpenItemEventHandler;
            _praxisProcessGuideEventHandler = praxisProcessGuideEventHandler;
            _praxisProcessGuideAnswerEventHandler = praxisProcessGuideAnswerEventHandler;
        }

        [Invocable]
        public bool Handle(GraphQlDataChangeEvent @event)
        {
            if (string.IsNullOrWhiteSpace(@event.EventTriggeredByJsonPayload))
                return false;

            var eventHandler = EventHandler(@event.EventType);

            return eventHandler != null && eventHandler.HandleAsync(@event).Result;
        }
        
        [Invocable]
        public async Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            if (string.IsNullOrWhiteSpace(@event.EventTriggeredByJsonPayload))
                return false;

            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return await eventHandler.HandleAsync(@event);
            }

            return false;
        }

        private IBaseEventHandlerAsync<GraphQlDataChangeEvent> EventHandler(string eventType)
        {
            if (EventTypeOf(EntityName.PraxisOrganization, eventType))
            {
                return _praxisOrganizationEventHandler;
            }
            if (EventTypeOf(EntityName.PraxisClientCategory, eventType))
            {
                return _praxisClientCategoryEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisClient, eventType))
            {
                return _praxisClientEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisUser, eventType))
            {
                return _praxisUserEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisForm, eventType))
            {
                return _praxisFormEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisTrainingAnswer, eventType))
            {
                return _praxisTrainingAnswerEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisTraining, eventType))
            {
                return _praxisTrainingEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisTaskConfig, eventType))
            {
                return _praxisTaskConfigEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisTask, eventType))
            {
                return _praxisTaskEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisRisk, eventType))
            {
                return _praxisRiskEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisAssessment, eventType))
            {
                return _praxisAssessmentEventHandler;
            }
            else if (EventTypeOf(EntityName.TaskSchedule, eventType))
            {
                return _taskScheduleCreatedEventHandler;
            }
            else if (EventTypeOf(EntityName.TaskSummary, eventType))
            {
                return _taskSummaryCreatedEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisEquipment, eventType))
            {
                return _praxisEquipmentEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisEquipmentMaintenance, eventType))
            {
                return _praxisEquipmentMaintenanceEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisRoom, eventType))
            {
                return _praxisRoomEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisOpenItemConfig, eventType))
            {
                return _praxisOpenItemConfigEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisOpenItem, eventType))
            {
                return _praxisOpenItemEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisOpenItemCompletionInfo, eventType))
            {
                return _praxisCompletionInfoEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisProcessGuide, eventType))
            {
                return _praxisProcessGuideEventHandler;
            }
            else if (EventTypeOf(EntityName.PraxisProcessGuideAnswer, eventType))
            {
                return _praxisProcessGuideAnswerEventHandler;
            }
            else return null;
        }

        private bool EventTypeOf(string entityName, string eventType)
        {
            return eventType.StartsWith(entityName + ".", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
