using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CirsReports;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class ExportReportCommand
    {
        public string ReportFileId { get; set; }
        public string FileNameWithExtension { get; set; }
        public string FileName { get; set; }
        public string ClientId { get; set; }
        public string OrganizationId { get; set; }
        public string FilterString { get; set; }
        public string ReportRemarks { get; set; }
        public DateTime? RequestedOn { get; set; }
        public int? TimezoneOffsetInMinutes { get; set; }
        public string LanguageKey { get; set; }
        public string ScheduleType { get; set; }
        public string CirsDashboardName { get; set; }
        public bool? IsActive { get; set; }
        public string CirsReportId { get; set; }
        public string TextSearchKey { get; set; }
        public DateFilter? CreateDateFilter { get; set; }
        public bool? IsCirsExternalAdmin { get; set; }
        public CirsDashboardName? DashboardNameEnum => !string.IsNullOrEmpty(CirsDashboardName) ? CirsDashboardName.EnumValue<CirsDashboardName>() : null;
    }
}
