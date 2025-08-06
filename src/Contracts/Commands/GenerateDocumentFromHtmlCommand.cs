namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class GenerateDocumentFromHtmlCommand
    {
        public string HtmlFileId { get; set; }
        public string PraxisReportFileId {get; set;}
        public string FileNameWithExtension {get; set;}
    }
}