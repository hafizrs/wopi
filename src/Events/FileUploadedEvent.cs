using SeliseBlocks.Genesis.Framework.Bus.Contracts.Event;

namespace Selise.Ecap.SC.PraxisMonitor.Events
{
    public class FileUploadedEvent : BlocksEvent
    {
        public string FileId { get; set; }
        public string FileVersionId { get; set; }
        public long SizeInBytes { get; set; }
        public string[] Tags { get; set; }
    }
}
