using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;

public interface IDeleteCirsReportsService
{
    Task<bool> InitiateDeletionAsync(DeleteCirsReportsCommand command);
    Task DeleteDataForClient(string clientId, string orgId = null);
}