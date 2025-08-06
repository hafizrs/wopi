using SeliseBlocks.Genesis.Framework.Bus.Contracts.Event;

namespace Selise.Ecap.ESignature.Service.Events
{
    public class ExternalContractProcessedEvent : BlocksEvent
    {
        public ProcessExternalSignResponse Response { get; set; }
    }

    public class ProcessExternalSignResponse
    {
        public string TrackingId { get; set; }
        public string DocumentId { get; set; }
        public string NextUrl { get; set; }
        public string AccessLink { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
