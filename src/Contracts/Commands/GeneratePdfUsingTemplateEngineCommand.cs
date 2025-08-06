using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class GeneratePdfUsingTemplateEngineCommand
    {
        public string FileNameWithExtension { get; set; }
        public string ClientId { get; set; }
        public string ModuleName  { get; set; }
        public string ReportFileId { get; set; }
        public string ReportRemarks { get; set; }
        public DateTime? RequestedOn { get; set; }
        public TemplateEnginePayload Payload { get; set; }
    }
}