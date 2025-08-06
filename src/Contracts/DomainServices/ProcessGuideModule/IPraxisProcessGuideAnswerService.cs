using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisProcessGuideAnswerService
    {
        Task UpdateProcessGuideLibraryFormResponse(ObjectArtifact artifact);
    }
}