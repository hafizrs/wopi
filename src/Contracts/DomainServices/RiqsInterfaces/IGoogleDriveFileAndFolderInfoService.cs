using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.IO;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{

    public interface IGoogleDriveFileAndFolderInfoService
    {
        Task<GoogleDriveFileDetails> GetFileInfo(string fileId, string accessToken);
        Task<byte[]> GetFileContentBytesAsync(string fileId, string fileExtension, string accessToken);
    }
}
