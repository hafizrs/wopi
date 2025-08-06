using EventHandlers.Models;
using Selise.Ecap.Entities.PrimaryEntities.StorageService;
using System.Collections.Generic;

namespace EventHandlers.Services
{
    public interface IFileService
    {
        File GetFileInformation(string fileId);
        List<ParentInfo> GetFileParentEntities(File file);
        List<File> GetConvertedFiles(string fileId);
    }
}
