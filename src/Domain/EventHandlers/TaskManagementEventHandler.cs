using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.TaskManagementEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.Events;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public partial class TaskManagementEventHandler : IEventHandler<TaskManagementEvent, bool>
    {
        private readonly TaskAssignedEventHandler assignedEventHandler;
        private readonly TaskOverdueEventHandler overdueEventHandler;
        private readonly TaskScheduleUpdateEventHandler _taskScheduleUpdateEventHandler;

        public TaskManagementEventHandler(
            TaskAssignedEventHandler assignedEventHandler,
            TaskOverdueEventHandler overdueEventHandler,
            TaskScheduleUpdateEventHandler taskScheduleUpdateEventHandler
        )
        {
            this.assignedEventHandler = assignedEventHandler;
            this.overdueEventHandler = overdueEventHandler;
            _taskScheduleUpdateEventHandler = taskScheduleUpdateEventHandler;
        }

        public bool Handle(TaskManagementEvent @event)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> HandleAsync(TaskManagementEvent @event)
        {
            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return await eventHandler.HandleAsync(@event);
            }

            return false;
        }

        public IBaseEventHandlerAsync<TaskManagementEvent> EventHandler(string eventType)
        {
            if (eventType.Equals(TaskManagementEventType.TaskAssignEvent))
            {
                return assignedEventHandler;
            }
            else if (eventType.Equals(TaskManagementEventType.TaskOverdueEvent))
            {
                return overdueEventHandler;
            }
            else if (eventType.Equals(TaskManagementEventType.TaskScheduleUpdateEvent))
            {
                return _taskScheduleUpdateEventHandler;
            }

            return null;
        }
    }
}
