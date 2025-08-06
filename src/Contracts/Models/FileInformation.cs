namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class FileInformation
    {
        public string FileName { get; set; }
        public string FileId { get; set; }
        public byte[] FileContent { get; set; }
    }
}