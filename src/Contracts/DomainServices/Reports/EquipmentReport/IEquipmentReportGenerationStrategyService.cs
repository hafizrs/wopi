using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.EquipmentReport;

public interface IEquipmentReportGenerationStrategyService
{
    IGenerateEquipmentListReport GetReportType(bool isReportForAllData);
}