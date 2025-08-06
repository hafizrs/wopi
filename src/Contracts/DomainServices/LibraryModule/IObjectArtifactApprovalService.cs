using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactApprovalService
    {
        Task<SearchResult> InitiateObjectArtifactApprovalProcess(ObjectArtifactApprovalCommand command);
    }
}