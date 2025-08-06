using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts
{
   public class PdfGeneratorPayload
    {
        public string MessageCoRelationId { get; set; }
        public IDictionary<string, string> EventReferenceData { get; set; }
        public PdfGeneratorConfiguration[] CreateFromHtmlCommands { get; set; }
    }
    public class PdfGeneratorConfiguration
    {
        public string HtmlFileId { get; set; }
        public string FooterHtmlFileId { get; set; }
        public string HeaderHtmlFileId { get; set; }
        public string DirectoryId { get; set; }
        public string OutputPdfFileId { get; set; }
        public string OutputPdfFileName { get; set; }
        public int HeaderHeight { get; set; }
        public int FooterHeight { get; set; }
        public bool IsPageNumberEnabled { get; set; }
        public bool IsTotalPageCountEnabled { get; set; }
        public bool UseFormatting { get; set; }
        public int Engine { get; set; }
        public string Profile { get; set; }
        public bool HasHeader { get; set; }
        public bool HasFooter { get; set; }
        public bool OpenInBrowser { get; set; }
    }
}
