using Selise.Ecap.SC.WopiMonitor.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.WopiMonitor.Contracts.Models.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Contracts.Queries.WopiModule;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.WopiMonitor.Contracts.DomainServices.WopiModule
{
    public interface IWopiService
    {
        Task<CreateWopiSessionResponse> CreateWopiSession(CreateWopiSessionCommand command);
        Task DeleteWopiSession(DeleteWopiSessionCommand command);
        Task LockWopiFile(LockWopiFileCommand command);
        Task<WopiFileInfo> GetWopiFileInfo(GetWopiFileInfoQuery query);
        Task<byte[]> GetWopiFileContent(GetWopiFileContentQuery query);
        Task UpdateWopiFile(UpdateWopiFileCommand command);
        List<WopiSessionResponse> GetWopiSessions(GetWopiSessionsQuery query);
        WopiSessionResponse GetWopiSession(GetWopiSessionQuery query);
        Task EnsureFileExists(string sessionId);
        Task<object> UploadFile(string sessionId, byte[] fileBuffer);
    }
} 