using System.Collections.Generic;

namespace EventHandlers.Services
{
    public interface IFileConversionService
    {
        void Convert(string fileId, List<string> fileTags, List<string> conversionTags);
        void AddConvertedFileMaps(string sourceFileId);

        void MarkToDeleteConvertedFileMaps(string orgFileId);
    }
}
