using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Events;

namespace EventHandlers.PdfGenerator
{
    public class PdfUsingTemplateEngineCompletedHandler : IEventHandler<PdfUsingTemplateEngineCompleted, bool>
    {
        public PdfUsingTemplateEngineCompletedHandler()
        {
        }
        public bool Handle(PdfUsingTemplateEngineCompleted @event)
        {
            throw new NotImplementedException();
        }

        public Task<bool> HandleAsync(PdfUsingTemplateEngineCompleted @event)
        {
            return Task.FromResult(true);
        }
    }
}
