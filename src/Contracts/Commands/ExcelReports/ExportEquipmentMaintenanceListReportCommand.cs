using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class ExportEquipmentMaintenanceListReportCommand: ExportReportCommand
    {
        public EquipmentMaintenanceListTranslation Translation { get; set; }
    }
}
