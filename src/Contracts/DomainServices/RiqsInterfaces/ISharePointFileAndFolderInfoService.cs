using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{
    
    public interface ISharePointFileAndFolderInfoService
    {
        Task<FolderDetails> GetFolderInfo(string siteId, string folderId);
        Task<FileDetails> GetFileInfo(string siteId, string fileId, string accessToken);
        Task<byte[]> GetFileContentBytesAsync(string fileUrl);
    }
}
