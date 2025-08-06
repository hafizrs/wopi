namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ObjectArtifactActivationDeactivationCommand
    {
        public string ObjectArtifactId { get; set; }
        public bool Activate { get; set; }
    }
}