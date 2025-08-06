using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

public class ExportSuppliersReportCommand : ExportReportCommand
{
    public SuppliersReportTranslation Translation { get; set; }
    public Dictionary<string, string> SupplierKeyNameTranslation { get; set; }
}

