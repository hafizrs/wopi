using System.Threading.Tasks;
using Selise.Ecap.ESignature.Service.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.Signature;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Signature
{
    public interface ISignatureService
    {
        Task<bool> CreateSignatureRequest(SignatureRequestCommand command);
        Task<bool> StoreSignedFileToLocal(string localFileStorageId, string signedFileId);
    }
}
