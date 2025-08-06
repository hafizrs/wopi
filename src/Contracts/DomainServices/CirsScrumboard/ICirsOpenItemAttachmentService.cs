using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;

public interface ICirsOpenItemAttachmentService
{
    Task UpdateCirsOnOpenItemCreate(string cirsReportId, string openItemId);
    Task UpdateCirsOpenItemCompletionStatus(string openItemId, PraxisKeyValue completionStatus);
}