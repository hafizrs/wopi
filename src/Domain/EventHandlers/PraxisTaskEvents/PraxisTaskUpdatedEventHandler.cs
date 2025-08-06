using System;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTaskEvents
{
    public class PraxisTaskUpdatedEventHandler : IBaseEventHandler<GqlEvent<PraxisTask>>
    {
        public bool Handle(GqlEvent<PraxisTask> eventPayload)
        {
            try
            {
               return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
