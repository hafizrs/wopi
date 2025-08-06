using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;

public interface ICirsAdminAssignedEventHandlerService
{
    Task<bool> InitiateAdminAssignedAfterEffectsAsync(string organizationId);
}