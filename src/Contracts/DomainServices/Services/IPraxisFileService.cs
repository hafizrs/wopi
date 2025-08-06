using Selise.Ecap.Entities.PrimaryEntities.StorageService;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisFileService
    {
        void UpdateStorageBaseUrl(string _baseUrl);
        File GetFileInformation(string fileId);
        Task<File> GetFileInfoFromStorage(string fileId);
        Task<File> GetFileInfoFromStorage(string fileId, string accessToken);
        Task <IEnumerable<EcapFile>> GetFilesInfoFromStorage(List<string> fileIds);
        Task<bool> DeleteFilesFromStorage(List<string> fileIds);
        List<PraxisParentInfo> GetFileParentEntities(File file);
        List<File> GetConvertedFiles(string fileId);
        Task<IEnumerable<ClonedFile>> CloneFiles(List<string> fileIds);
        Task<bool> DeleteFilesFromStorage(List<string> fileIds, string accessToken);
        Task<CreateDocumentEditUrlResponse> CreateDocumentEditUrl(CreateDocumentEditUrlPayload payload);

    }
}
