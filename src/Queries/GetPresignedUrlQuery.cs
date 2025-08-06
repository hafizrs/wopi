using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Queries
{
   public class GetPresignedUrlQuery
    {
        public List<FileInfoData> FileInfoList { get; set; }
    }
    public class FileInfoData
    {
        public string FileId { get; set; }
        public string FileName { get; set; }
    }
}
