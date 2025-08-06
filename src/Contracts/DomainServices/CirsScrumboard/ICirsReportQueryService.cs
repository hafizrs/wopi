using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CirsReports;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;

public interface ICirsReportQueryService
{
    Task<List<CirsReportResponse>> GetReportsAsync(GetCirsReportQuery query);
    Task<List<CirsGenericReport>> GetReportsAsync(GetCirsReportByIdsQuery query);
    Task<List<CirsGenericReport>> GetFaultReportsAsync(GetFaultReportByEquipmentIdQuery query);
}
