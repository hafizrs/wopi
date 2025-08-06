using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Threading.Tasks;
using Selise.Ecap.ESignature.Service.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.Entities.PrimaryEntities.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryFormService
    {
        Task LibraryFormClone(LibraryFormCloneCommand command);
        Task CreateStandardLibraryForm(CreateStandardLibraryFormCommand command);
        Task<LibraryFormMappingRecord> GetFormCloneMappingRecord(string objectArtifactId);
        Task UpdateForm(LibraryFormUpdateCommand command);
        Task<bool> UpdateFormSignatureUrl(ExternalContractProcessedEvent @event);
        Task<bool> CompleteFormSignature(ExternalContractSentAndSignedEvent @event);
        Task<FormSignatureMapping> GetFormSignatureMapping(string objectArtifactId);
        Task GenerateSignatureUrl(string objectArtifactId);
        Task<ObjectArtifact> GetFormObjectArtifactById(string objectArtifactId);
        Task UpdateArtifactWithEvent(ObjectArtifact objectArtifactData);
    }
}
