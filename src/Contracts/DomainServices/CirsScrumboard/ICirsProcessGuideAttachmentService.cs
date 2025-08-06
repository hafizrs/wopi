using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;

public interface ICirsProcessGuideAttachmentService
{
    Task UpdateOnProcessGuideCreatedAsync(string cirsReportId, string processGuideId);
    Task UpdateProcessGuideCompletionStatus(string processGuideId, int completionStatus);
}
