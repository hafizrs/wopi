using Selise.Ecap.Entities.PrimaryEntities.StorageService;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisFileService
    {
        File GetFileInformation(string fileId);
        Task<File> GetFileInfoFromStorage(string fileId);
        Task<File> GetFileInfoFromStorage(string fileId, string accessToken);
        Task <IEnumerable<EcapFile>> GetFilesInfoFromStorage(List<string> fileIds);
        Task<bool> DeleteFilesFromStorage(List<string> fileIds);
        Task<IEnumerable<ClonedFile>> CloneFiles(List<string> fileIds);
        Task<bool> DeleteFilesFromStorage(List<string> fileIds, string accessToken);

    }
}
