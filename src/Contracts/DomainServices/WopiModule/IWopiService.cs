using Selise.Ecap.SC.Wopi.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.EntityResponse;
using Selise.Ecap.SC.Wopi.Contracts.Models.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.Queries.WopiModule;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.Wopi.Contracts.DomainServices.WopiModule
{
    public interface IWopiService
    {
        Task<CreateWopiSessionResponse> CreateWopiSession(CreateWopiSessionCommand command);
        Task DeleteWopiSession(DeleteWopiSessionCommand command);
        Task<(Stream fileStream, string fileName)> LockWopiFile(LockWopiFileCommand command);
        Task<WopiFileInfo> GetWopiFileInfo(GetWopiFileInfoQuery query);
        Task<Stream> GetWopiFileContent(GetWopiFileContentQuery query);
        Task<(Stream fileStream, string fileName)> GetWopiFileContentDirect(string sessionId, string accessToken);
        Task<UpdateWopiFileResponse> UpdateWopiFile(UpdateWopiFileCommand command);
        List<WopiSessionResponse> GetWopiSessions(GetWopiSessionsQuery query);
        WopiSessionResponse GetWopiSession(GetWopiSessionQuery query);
        Task EnsureFileExists(string sessionId);
        Task<object> UploadFile(string sessionId, byte[] fileBuffer);
        Task<bool> UploadFileToUrl(UploadFileToUrlCommand command);
    }
} 