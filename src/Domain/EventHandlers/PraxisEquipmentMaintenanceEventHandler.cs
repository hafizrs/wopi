using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.EquipmentMaintenanceEvents;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisEquipmentMaintenanceEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly EquipmentMaintenanceCreatedEventHandler createdEventHandler;
        private readonly EquipmentMaintenanceUpdatedEventHandler updatedEventHandler;

        public PraxisEquipmentMaintenanceEventHandler(
            EquipmentMaintenanceCreatedEventHandler createdEventHandler,
            EquipmentMaintenanceUpdatedEventHandler updatedEventHandler
        )
        {
            this.createdEventHandler = createdEventHandler;
            this.updatedEventHandler = updatedEventHandler;
        }
        public Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisEquipmentMaintenance>>(@event.EventTriggeredByJsonPayload);

            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return Task.FromResult(eventHandler.Handle(eventPayload));
            }

            return Task.FromResult(false);
        }

        public IBaseEventHandler<GqlEvent<PraxisEquipmentMaintenance>> EventHandler(string eventType)
        {
            if (eventType.Equals(PraxisEventName.PraxisEquipmentMaintenanceCreatedEventName))
            {
                return createdEventHandler;
            }
            else if (eventType.Equals(PraxisEventName.PraxisEquipmentMaintenanceUpdatedEventName))
            {
                return updatedEventHandler;
            }

            return null;
        }
    }
}
