namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ObjectArtifactRenameCommand
    {
        public string ObjectArtifactId { get; set; }
        public string Name { get; set; }
        public string ViewMode { get; set; }
    }

}