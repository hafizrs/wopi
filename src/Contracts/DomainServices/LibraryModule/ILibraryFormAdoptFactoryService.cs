using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Threading.Tasks;
using Selise.Ecap.ESignature.Service.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.Entities.PrimaryEntities.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryFormAdoptFactoryService
    {
        Task AdoptLibraryFormResponse(ObjectArtifact artifact);
    }
}
