using System.Collections.Generic;
using SeliseBlocks.Genesis.Framework.Bus.Contracts.Event;

namespace Selise.Ecap.SC.PraxisMonitor.Events
{
    public class PdfsFromHtmlCreatedEvent : BlocksEvent, IEvent
    {
        public bool Success { get; set; }
        public string MessageCoRelationId { get; set; }
        public IDictionary<string, string> EventReferenceData { get; set; }
        public string OutputFileId { get; set; }
    }
    public interface IEvent
    {
        bool Success { get; set; }
        string MessageCoRelationId { get; set; }
    }
}
