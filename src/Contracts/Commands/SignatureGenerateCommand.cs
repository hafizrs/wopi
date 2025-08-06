using System.ComponentModel.DataAnnotations;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class SignatureGenerateCommand
    {
        [Required] public string ObjectArtifactId { get; set; }
    }
}
