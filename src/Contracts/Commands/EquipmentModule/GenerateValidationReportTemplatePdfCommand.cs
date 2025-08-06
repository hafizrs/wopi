using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule
{
    public class GenerateValidationReportTemplatePdfCommand
    {
        public string FileNameWithExtension { get; set; }
        public string ModuleName { get; set; }
        public string ReportFileId { get; set; }
        public string HeaderHtmlFileId { get; set; }
        public string FooterHtmlFileId { get; set; }
        public string TemplateFileId { get; set; }
        public string FilterString { get; set; }
        public string SortedBy { get; set; }
        public Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
        public string EquipmentId{ get; set; } = string.Empty;
        public string PreparationReportItemId { get; set; }
        public string GeneralReportItemId { get; set; }
        public string OutputPdfFileId { get; set; }
    }
}
