namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class MoveFileCommand
    {
        public string SourceFileId { get; set; }

        public string TargetFileName { get; set; }
    }
}
