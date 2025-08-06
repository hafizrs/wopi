using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Domain.Events;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Users;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class UserEventHandler : IEventHandler<UserEvent, bool>
    {
        private readonly UserActivatedEventHandler activatedEventHandler;

        public UserEventHandler(
            UserActivatedEventHandler activatedEventHandler)
        {
            this.activatedEventHandler = activatedEventHandler;
        }

        [InvocableAttribute]
        public bool Handle(UserEvent @event)
        {
            var data = JsonConvert.DeserializeObject<User>(@event.JsonPayload);

            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return eventHandler.Handle(data);
            }

            return true;
        }

        public Task<bool> HandleAsync(UserEvent @event)
        {
            throw new NotImplementedException();
        }

        private IBaseEventHandler<User> EventHandler(string eventType)
        {
            if (eventType.Equals(PraxisEventName.UserActivatedEventName))
            {
                return activatedEventHandler;
            }
            return null;
        }
    }
}
