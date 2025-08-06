namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class ConversionPipelinePayload
    {
        public string ConversionPipelineId { get; set; }
        public string TaskId { get; set; }
        public RequestProperties RequestProperties { get; set; }
    }

    public class RequestProperties
    {
        public ImageResizeSetting ImageResizeSetting { get; set; }
    }
}
