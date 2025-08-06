using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.Events;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisProcessGuideEvents
{
    public class UpdateProcessGuideFromSyncEventHandler: IEventHandler<UpdateProcessGuideCompletionStatusEvent, bool>
    {
        private readonly IPraxisProcessGuideService _processGuideService;
        public UpdateProcessGuideFromSyncEventHandler(IPraxisProcessGuideService processGuideService)
        {
            _processGuideService = processGuideService;
        }
        public bool Handle(UpdateProcessGuideCompletionStatusEvent @event)
        {
            throw new System.NotImplementedException();
        }

        public async Task<bool> HandleAsync(UpdateProcessGuideCompletionStatusEvent @event)
        {
            return await _processGuideService.UpdateProcessGuideCompletionStatus(@event.ProcessGuideIds);
        }
    }
}