using System;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTrainingAnswerEvents
{
    public class PraxisTrainingAnswerUpdatedEventHandler : IBaseEventHandler<GqlEvent<PraxisTrainingAnswer>>
    {
        public bool Handle(GqlEvent<PraxisTrainingAnswer> eventPayload)
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
