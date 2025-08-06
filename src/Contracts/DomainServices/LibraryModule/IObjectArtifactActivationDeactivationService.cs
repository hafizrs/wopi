using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactActivationDeactivationService
    {
        Task<bool> InitiateObjectArtifactActivationDeactivationProcess(ObjectArtifactActivationDeactivationCommand command);
    }
}