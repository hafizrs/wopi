using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IGenerateEquipmentMaintenanceListReport
    {
        Task<bool> PrepareEquipmentMaintenanceListReport(string filter, PraxisClient client,
            ExcelPackage excel, EquipmentMaintenanceListTranslation translationEquipmentList, bool isValidationReport);
    }
}
