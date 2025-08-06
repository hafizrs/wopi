using SeliseBlocks.Genesis.Framework.Bus.Contracts.Event;

namespace Selise.Ecap.SC.PraxisMonitor.Events
{
   public class PdfUsingTemplateEngineCompleted: BlocksEvent
    {
        public string RequestId { get; set; }
        public bool IsSucceed { get; set; }
        public string PdfFileId { get; set; }
    }
}
