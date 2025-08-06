using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.EquipmentReport;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.EquipmentReport;

public class EquipmentReportGenerationStrategyService : IEquipmentReportGenerationStrategyService
{
    private readonly GenerateEquipmentListReportForSingleClient _generateEquipmentListReportForSingleClient;
    private readonly GenerateEquipmentListReportForMultipleClient _generateEquipmentListReportForMultipleClient;

    public EquipmentReportGenerationStrategyService(
        GenerateEquipmentListReportForSingleClient generateEquipmentListReportForSingleClient,
        GenerateEquipmentListReportForMultipleClient generateEquipmentListReportForMultipleClient
    )
    {
        _generateEquipmentListReportForSingleClient = generateEquipmentListReportForSingleClient;
        _generateEquipmentListReportForMultipleClient = generateEquipmentListReportForMultipleClient;
    }

    public IGenerateEquipmentListReport GetReportType(bool isReportForAllData)
    {
        return isReportForAllData
            ? _generateEquipmentListReportForMultipleClient
            : _generateEquipmentListReportForSingleClient;
    }

    
}