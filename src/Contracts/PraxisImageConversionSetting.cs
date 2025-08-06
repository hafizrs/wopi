using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts
{
    public class PraxisImageConversionSetting
    {
        public string SourceFileId { get; set; }
        public Dimension[] Dimensions { get; set; }
        public ParentEntity[] ParentEntities { get; set; }
        public bool KeepCanvasSameWithImage { get; set; }
        public bool UseJpegEncoding { get; set; }
    }
}
