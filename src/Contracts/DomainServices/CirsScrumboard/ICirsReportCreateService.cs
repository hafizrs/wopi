using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;

public interface ICirsReportCreateService
{
    Task InitiateReportCreationAsync(AbstractCreateCirsReportCommand command);
    Task DuplicateCirsReport(CirsGenericReport cirsReport, Dictionary<string, object> cirsReportUpdates);
}
